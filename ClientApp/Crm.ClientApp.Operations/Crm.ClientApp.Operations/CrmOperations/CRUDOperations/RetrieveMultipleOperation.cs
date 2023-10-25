using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Crm.Common;

namespace Crm.ClientApp.Operations.CrmOperations
{
    public class RetrieveMultipleOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            RetrieveMultipleRecordsUsingQueryByAttribute();
            RetrieveMultipleRecordsUsingQueryExpression();
            RetrieveMultipleRecordsUsingQueryExpressionWithColumnComparison();
            RetrieveMultipleRecordsUsingQueryExpressionWithLinkEntity();
            RetrieveRecentLeadsNoPhoneCall();
        }

        /// <summary>
        /// RetrieveMultiple   QueryByAttribute
        /// </summary>
        /// <returns></returns>
        public EntityCollection RetrieveMultipleRecordsUsingQueryByAttribute()
        {
            var query = new QueryByAttribute
            {
                EntityName = "account",
                ColumnSet = new ColumnSet("name"),
                Attributes = { "statecode", "donotemail" },
                Values = { 0, false },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 10
            };

            var result = crmServiceClient.RetrieveMultiple(query);
            result.PrintEntityCollection();

            return result;
        }

        /// <summary>
        /// RetrieveMultiple  QueryExpression
        /// </summary>
        /// <returns></returns>
        public EntityCollection RetrieveMultipleRecordsUsingQueryExpression()
        {
            var query = new QueryExpression
            {
                EntityName = "account",
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression()
                {
                    Conditions =
                    {
                        new ConditionExpression("statecode",  ConditionOperator.Equal,0),
                        new ConditionExpression("donotemail",  ConditionOperator.Equal,false),
                        new ConditionExpression("name", ConditionOperator.NotNull),
                        new ConditionExpression("createdon", ConditionOperator.ThisYear)
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 10
            };

            var result = crmServiceClient.RetrieveMultiple(query);
            result.PrintEntityCollection();

            return result;
        }

        /// <summary>
        ///  RetrieveMultiple  QueryExpression ColumnComparison
        /// </summary>
        /// <returns></returns>
        public EntityCollection RetrieveMultipleRecordsUsingQueryExpressionWithColumnComparison()
        {
            var query = new QueryExpression
            {
                EntityName = "systemuser",
                ColumnSet = new ColumnSet("fullname"),
                Criteria = new FilterExpression()
                {
                    Conditions =
                    {
                        new ConditionExpression("isdisabled",  ConditionOperator.Equal, false),
                        new ConditionExpression("firstname", ConditionOperator.Equal, true, "lastname") //firstname == lastname
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 10
            };

            var result = crmServiceClient.RetrieveMultiple(query);
            result.PrintEntityCollection("fullname");

            return result;
        }

        /// <summary>
        /// RetrieveMultiple  QueryExpression Link Entity
        /// Example,  contact=>parent account=>primary contact
        /// </summary>
        /// <returns></returns>
        public EntityCollection RetrieveMultipleRecordsUsingQueryExpressionWithLinkEntity()
        {
            var query = new QueryExpression
            {
                EntityName = "contact",
                ColumnSet = new ColumnSet("fullname"),
                Criteria = new FilterExpression()
                {
                    Conditions =
                    {
                        new ConditionExpression("statecode",  ConditionOperator.Equal, 0),
                        new ConditionExpression("parentcustomerid", ConditionOperator.NotNull)
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 5
            };

            var linkEntityAccount = new LinkEntity("contact", "account", "parentcustomerid", "accountid", JoinOperator.Inner)
            {
                EntityAlias = "parentaccount",
                Columns = new ColumnSet("name"),
            };

            var linkEntityPrimaryContact = new LinkEntity("account", "contact", "primarycontactid", "contactid", JoinOperator.Inner)
            {
                EntityAlias = "parentaccount.primarycontact",
                Columns = new ColumnSet("fullname"),
            };

            linkEntityAccount.LinkEntities.Add(linkEntityPrimaryContact);
            query.LinkEntities.Add(linkEntityAccount);

            var result = crmServiceClient.RetrieveMultiple(query);
            foreach (var entity in result.Entities)
                entity.PrintAttributes();

            return result;
        }

        /// <summary>
        /// Retrieve recent created leads who has no related phone call
        /// </summary>
        /// <returns></returns>
        public EntityCollection RetrieveRecentLeadsNoPhoneCall()
        {
            var query = new QueryExpression
            {
                EntityName = "lead",
                ColumnSet = new ColumnSet("subject"),
                Criteria = new FilterExpression()
                {
                    Conditions =
                    {
                        new ConditionExpression("statecode",  ConditionOperator.Equal, 0),
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 5
            };

            var linkEntity = new LinkEntity("lead", "phonecall", "leadid", "regardingobjectid", JoinOperator.LeftOuter)
            {
                EntityAlias = "parentaccount",
                LinkCriteria = new FilterExpression()
                {
                    Conditions = { new ConditionExpression("activityid", ConditionOperator.Null) }
                }
            };

            query.LinkEntities.Add(linkEntity);

            var result = crmServiceClient.RetrieveMultiple(query);
            result.PrintEntityCollection("subject");
            return result;
        }
    }
}