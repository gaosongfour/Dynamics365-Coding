using System;
using Microsoft.Xrm.Sdk.Query;
using Crm.Common.Query;

namespace Crm.ClientApp.Operations.CrmOperations
{
    public class RetrieveMultipleUsingPagingOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            RetrieveAllAccounts();
            RetrieveAllAccountsByFetchXml();
        }

        private void RetrieveAllAccounts()
        {
            var basicQuery = new QueryExpression
            {
                EntityName = "account",
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression()
                {
                    Conditions =
                    {
                        new ConditionExpression("statecode",  ConditionOperator.Equal,0),
                        new ConditionExpression("name", ConditionOperator.NotNull),
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
            };

            var entityList = crmServiceClient.RetrieveAllRecords(basicQuery);
            Console.WriteLine($"Total accounts=>{entityList.Count}");
        }

        private void RetrieveAllAccountsByFetchXml()
        {
            var fetchXml = @"
            <fetch mapping='logical'>
              <entity name='account'>
                <attribute name='name'/>
                <filter>
                  <condition attribute='statecode' operator='eq' value='0'/>
                  <condition attribute='createdon' operator='last-x-years' value='10'/>
                </filter>
                <order attribute='createdon' descending='true'/>
              </entity>
            </fetch>
            ";

            var entityList = crmServiceClient.RetrieveAllRecordsByFetchXml(fetchXml);
            Console.WriteLine($"Total accounts=>{entityList.Count}");
        }
    }
}
