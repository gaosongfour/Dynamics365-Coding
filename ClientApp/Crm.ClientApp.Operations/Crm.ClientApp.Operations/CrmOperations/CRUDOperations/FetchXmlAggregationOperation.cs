using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Crm.Common;
using System.Text;

namespace Crm.ClientApp.Operations.CrmOperations
{
    public class FetchXmlAggregationOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            GetTotalContacts();
            GetTotalContactsGroupByAccount();
            GetLeadTotalAmountGroupByAccount();
        }

        private void GetTotalContacts()
        {
            var query = @"
            <fetch distinct='false' mapping='logical' aggregate='true'>
               <entity name='contact'>
                  <attribute name='contactid' aggregate='count' alias='totalContact'/>
                  <filter>
                     <condition attribute='createdon' operator='last-x-years' value='3'/>
                  </filter>
               </entity>
            </fetch>
            ";

            var result = crmServiceClient.RetrieveMultiple(new FetchExpression(query));
            var total = result.Entities[0].GetAliasedAttributeValue<int>("totalContact");
            Console.WriteLine($"Total contacts created in last 3 years=>{total}");
        }

        private void GetTotalContactsGroupByAccount()
        {
            var query = @"
            <fetch distinct='false' mapping='logical' aggregate='true'>
               <entity name='contact'>
                  <attribute name='contactid' aggregate='count' alias='totalContact'/>
                  <filter>
                     <condition attribute='createdon' operator='last-x-years' value='3'/>
                     <condition attribute='parentcustomerid' operator='not-null'/>
                  </filter>
                  <link-entity name='account' from='accountid' to='parentcustomerid'>
                     <attribute name='name' alias='accountName' groupby='true' />
                      <order alias='accountName' descending='false'/>
                  </link-entity>                 
               </entity>
            </fetch>
            ";

            var result = crmServiceClient.RetrieveMultiple(new FetchExpression(query));
            Console.WriteLine("Total Contacts By Account");
            foreach (var entity in result.Entities)
                Console.WriteLine($" Total contacts for account ({entity.GetAliasedAttributeValue<string>("accountName")})=>{entity.GetAliasedAttributeValue<int>("totalContact")}");
        }

        private void GetLeadTotalAmountGroupByAccount()
        {
            var query = @"
            <fetch distinct='false' mapping='logical' aggregate='true'>
               <entity name='lead'>
                  <attribute name='estimatedamount' aggregate='sum' alias='totalEstimatedAmount'/>
                  <attribute name='leadid' aggregate='count' alias='totalLeadCount'/>
                  <filter>
                     <condition attribute='parentaccountid' operator='not-null'/>
                  </filter>
                  <link-entity name='account' from='accountid' to='parentaccountid'>
                     <attribute name='name' alias='accountName' groupby='true' />
                     <order alias='accountName' descending='false'/>
                  </link-entity>
               </entity>
            </fetch>
            ";

            var result = crmServiceClient.RetrieveMultiple(new FetchExpression(query));
            Console.WriteLine("Total Lead Estimation Amount By Account");
            foreach (var entity in result.Entities)
                Console.WriteLine($"{entity.GetAliasedAttributeValue<string>("accountName")}| Leads count=>{entity.GetAliasedAttributeValue<int>("totalLeadCount")}|Total Estimation Amount=>{entity.GetAliasedAttributeValue<Money>("totalEstimatedAmount").Value}");
        }
    }
}
