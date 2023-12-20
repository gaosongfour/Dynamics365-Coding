using System;
using Microsoft.Xrm.Tooling.Connector;
using System.Configuration;
using Crm.Common.CrmService;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

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

        #region Retrieve Test Records
        protected Entity RetrieveLatestAccount()
        {
            var query = new QueryByAttribute
            {
                EntityName = "account",
                ColumnSet = new ColumnSet("name"),
                Attributes = { "statecode" },
                Values = { 0 },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 1
            };

            var result = crmServiceClient.RetrieveMultiple(query);
            return result.Entities.FirstOrDefault();
        }

        protected Entity RetrieveLatestContact()
        {
            var query = new QueryByAttribute
            {
                EntityName = "contact",
                ColumnSet = new ColumnSet("fullname"),
                Attributes = { "statecode" },
                Values = { 0 },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 1
            };

            var result = crmServiceClient.RetrieveMultiple(query);
            return result.Entities.FirstOrDefault();
        }

        protected Entity RetrieveLastestClient()
        {
            var query = new QueryByAttribute
            {
                EntityName = "new_client",
                ColumnSet = new ColumnSet("new_name"),
                Attributes = { "statecode" },
                Values = { 0 },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 1
            };

            var result = crmServiceClient.RetrieveMultiple(query);
            return result.Entities.FirstOrDefault();
        }

        protected Entity RetrieveLastestFactory()
        {
            var query = new QueryByAttribute
            {
                EntityName = "new_factory",
                ColumnSet = new ColumnSet("new_name"),
                Attributes = { "statecode" },
                Values = { 0 },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 1
            };

            var result = crmServiceClient.RetrieveMultiple(query);
            return result.Entities.FirstOrDefault();
        }
        #endregion
    }
}