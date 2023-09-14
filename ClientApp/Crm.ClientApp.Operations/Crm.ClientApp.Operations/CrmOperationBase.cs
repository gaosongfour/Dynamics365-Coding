using System;
using Microsoft.Xrm.Tooling.Connector;
using System.Configuration;
using Crm.Common.CrmService;
using Microsoft.Crm.Sdk.Messages;

namespace Crm.ClientApp.Operations
{
    public class CrmOperationBase
    {
        protected CrmServiceClient crmServiceClient;

        public CrmOperationBase()
        {
            var crmConfigPath = ConfigurationManager.AppSettings["crmConfigPath"];
            var crmConnString = CrmConfigHelper.GetCrmConnString(crmConfigPath);
            crmServiceClient = CrmServiceHelper.GetCrmServiceClient(crmConnString);
            Console.WriteLine($"crm connected to {crmServiceClient.ConnectedOrgFriendlyName}");
        }

        protected Guid WhoAmI()
        {
            var request = new WhoAmIRequest();
            var response = (WhoAmIResponse)crmServiceClient.Execute(request);

            Console.WriteLine($"user connected=>{response.UserId}");
            return response.UserId;
        }
    }
}