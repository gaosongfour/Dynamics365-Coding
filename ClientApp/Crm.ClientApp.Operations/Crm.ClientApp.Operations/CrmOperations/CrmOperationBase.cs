using System;
using Microsoft.Xrm.Tooling.Connector;
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

        protected CrmConfig crmConfig;

        public CrmOperationBase()
        {
            crmConfig = CrmConfigHelper.GetCrmConfig();
            crmServiceClient = CrmServiceHelper.GetCrmServiceClient(crmConfig.crmConnString);
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

        #region Get Entity Record Url
        /// <summary>
        /// Get Crm Record Url
        /// </summary>
        /// <param name="entityName">entity logical name</param>
        /// <param name="entityId">entity id</param>
        /// <returns></returns>
        protected string GetCrmRecordUrl(string entityName, string entityId)
        {
            var baseUrl = crmServiceClient.CrmConnectOrgUriActual.Authority;
            return $"https://{baseUrl}/main.aspx?etn={entityName}&pagetype=entityrecord&id=%7B{entityId}%7D ";
        }

        /// <summary>
        ///  Get Crm Record Url
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected string GetCrmRecordUrl(Entity entity)
        {
            var baseUrl = crmServiceClient.CrmConnectOrgUriActual.Authority;
            return $"https://{baseUrl}/main.aspx?etn={entity.LogicalName}&pagetype=entityrecord&id=%7B{entity.Id}%7D ";
        }
        #endregion
    }
}