using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Xrm.Sdk.Messages;

namespace Crm.ClientApp.Operations.CrmOperations
{
    public class RetrieveWithRelatedDataOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            var accountEntity = RetrieveSampleAccount();
            if (accountEntity == null)
            {
                Console.WriteLine("no account retrieved");
                return;
            }

            Console.WriteLine($"account {accountEntity.Id} retrieved");

            RetrieveAccountRecordWithRelatedData(accountEntity.Id);
        }

        /// <summary>
        /// Retrieve an account record with owner info and related child contacts
        /// </summary>
        /// <param name="accountId"></param>
        public void RetrieveAccountRecordWithRelatedData(Guid accountId)
        {
            //account owner(systemuser)
            var relationAccountOwner = new Relationship("user_accounts");
            var queryOwner = new QueryExpression() { EntityName = "systemuser", ColumnSet = new ColumnSet("domainname") };
            //account contacts by parentcustomerid
            var relationAccountChildContact = new Relationship("contact_customer_accounts");
            var queryContact = new QueryExpression() { EntityName = "contact", ColumnSet = new ColumnSet("fullname") };

            var request = new RetrieveRequest();
            request.Target = new EntityReference("account", accountId);
            request.ColumnSet = new ColumnSet("name");
            request.RelatedEntitiesQuery = new RelationshipQueryCollection();
            request.RelatedEntitiesQuery.Add(relationAccountOwner, queryOwner);
            request.RelatedEntitiesQuery.Add(relationAccountChildContact, queryContact);

            var result = (RetrieveResponse)crmServiceClient.Execute(request);
            var accountEntity = result.Entity;

            Console.WriteLine($"account name=>{accountEntity.GetAttributeValue<string>("name")}");

            if (accountEntity.RelatedEntities.ContainsKey(relationAccountOwner))
            {
                var ownerEntity = accountEntity.RelatedEntities[relationAccountOwner].Entities[0];
                Console.WriteLine($"account owner=>{ownerEntity.GetAttributeValue<string>("domainname")}");
            }

            if (accountEntity.RelatedEntities.ContainsKey(relationAccountChildContact))
            {
                foreach (var contactEntity in accountEntity.RelatedEntities[relationAccountChildContact].Entities)
                    Console.WriteLine($"contact=>{contactEntity.GetAttributeValue<string>("fullname")}");
            }
        }

        private Entity RetrieveSampleAccount()
        {
            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                         new ConditionExpression("statecode",  ConditionOperator.Equal, 0),
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 1
            };

            var linkEntity = query.AddLink("contact", "accountid", "parentcustomerid", JoinOperator.LeftOuter);
            linkEntity.EntityAlias = "child_contact";
            query.Criteria.AddCondition("child_contact", "contactid", ConditionOperator.NotNull);

            var result = crmServiceClient.RetrieveMultiple(query);
            return result.Entities.FirstOrDefault();
        }
    }
}
