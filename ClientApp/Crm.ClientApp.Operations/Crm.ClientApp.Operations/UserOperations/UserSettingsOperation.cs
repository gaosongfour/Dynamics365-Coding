using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;

namespace Crm.ClientApp.Operations.UserOperations
{
    public class UserSettingsOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            var userId = WhoAmI();
            RetrieveUserLicense(userId);
        }

        public List<ServicePlan> RetrieveUserLicense(Guid userId)
        {
            var request = new RetrieveUserLicenseInfoRequest { SystemUserId = userId };
            var response = (RetrieveUserLicenseInfoResponse)crmServiceClient.Execute(request);
            var licenseInfo = response.licenseInfo;

            Console.WriteLine($"User with Id {userId} License info");
            foreach (var servicePlan in licenseInfo.ServicePlans)
                Console.WriteLine($"{servicePlan.DisplayName}({servicePlan.Name})");

            return licenseInfo.ServicePlans;
        }
    }
}