using System;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Tooling.Connector;
using System.ServiceModel.Description;
using Crm.Common.CrmService;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Crm.ClientApp.Operations.CrmOperations
{
    public class OrganizationOperation : ICrmOperation
    {
        public void Execute()
        {
            var crmConfig = CrmConfigHelper.GetCrmConfig();
            var orgDetailCollection = GetGlobalOrganizations(crmConfig);

            int number = 1;
            while (orgDetailCollection.Count > 0)
            {
                Console.WriteLine($"Please type the number of organization to be connected to (1 to {orgDetailCollection.Count})");
                var input = Console.ReadLine();

                var parseResult = int.TryParse(input, out number);
                if (parseResult == true && number >= 1 && number < orgDetailCollection.Count)
                    break;
            }

            //Set selected organization as  the crmUrl
            var orgDetail = orgDetailCollection[number - 1];
            crmConfig.crmUrl = orgDetail.Endpoints[EndpointType.WebApplication];
            var connectedCrmServiceClient = new CrmServiceClient(crmConfig.crmConnString);
            if (connectedCrmServiceClient.IsReady)
            {
                ShowOrganizationInfo(connectedCrmServiceClient.OrganizationDetail);
            }
            else
            {
                Console.WriteLine($"Crm Service Client Init Error=>{connectedCrmServiceClient.LastCrmError}");
            }
        }

        private OrganizationDetailCollection GetGlobalOrganizations(CrmConfig crmConfig)
        {
            var clientCredential = new ClientCredentials();
            clientCredential.UserName.UserName = crmConfig.userName;
            clientCredential.UserName.Password = crmConfig.userPwd;

            var orgDetailCollection = CrmServiceClient.DiscoverGlobalOrganizations(
                discoveryServiceUri: new Uri(crmConfig.discoveryServiceUri),
                clientCredentials: clientCredential,
                user: null,
                clientId: crmConfig.crmAppId,
                redirectUri: new Uri(crmConfig.redirectUri),
                tokenCachePath: "",
                isOnPrem: false,
                authority: "",
                promptBehavior: PromptBehavior.Auto);

            Console.WriteLine($"{orgDetailCollection.Count} organizations retrieved");
            var number = 1;
            foreach (var orgDetail in orgDetailCollection)
            {
                Console.WriteLine($"No.{number}=>{orgDetail.FriendlyName}");
                number++;
            }

            return orgDetailCollection;
        }

        private void ShowOrganizationInfo(OrganizationDetail orgDetail)
        {
            Console.WriteLine("Organization currently connected to:");
            Console.WriteLine($"Name=>{orgDetail.FriendlyName}");
            Console.WriteLine($"Id=>{orgDetail.OrganizationId}");
            Console.WriteLine($"Version=>{orgDetail.OrganizationVersion}");
            Console.WriteLine($"WebApplication Endpoint=>{orgDetail.Endpoints[EndpointType.WebApplication]}");
        }
    }
}
