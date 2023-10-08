using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace Crm.ClientApp.Operations.UserOperations
{
    public class UserSettingsOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            var userId = WhoAmI();
            RetrieveUserSettings(userId);
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

        public void RetrieveUserSettings(Guid userId)
        {
            var query = new QueryByAttribute
            {
                EntityName = "usersettings",
                ColumnSet = new ColumnSet(true),
                Attributes = { "systemuserid" },
                Values = { userId }
            };

            var result = crmServiceClient.RetrieveMultiple(query);
            if (result.Entities.Count == 1)
            {
                var userSettingsEntity = result.Entities.First();
                var text = $@"user settigns for user {userId}:
currency symbol=>{userSettingsEntity.GetAttributeValue<string>("currencysymbol")}
date format string=>{userSettingsEntity.GetAttributeValue<string>("dateformatstring")}
decimal symbol=>{userSettingsEntity.GetAttributeValue<string>("decimalsymbol")}
paging limit=>{userSettingsEntity.GetAttributeValue<int>("paginglimit")}
time zone code=>{userSettingsEntity.GetAttributeValue<int>("timezonecode")}
ui languageid=>{userSettingsEntity.GetAttributeValue<int>("uilanguageid")}";
                Console.WriteLine(text);
            }
            else
            {
                Console.WriteLine($"user settigns for user {userId} not found");
            }
        }
    }
}