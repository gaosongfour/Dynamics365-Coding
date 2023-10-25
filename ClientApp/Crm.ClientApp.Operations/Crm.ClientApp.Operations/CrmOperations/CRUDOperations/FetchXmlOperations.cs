using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Crm.Common;

namespace Crm.ClientApp.Operations.CrmOperations
{
    /// <summary>
    /// Basic retrive multiple operation using FetchXml, same query result as RetrieveMultipleOperation
    /// </summary>
    public class FetchXmlOperations : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            RetrieveMultipleRecordsUsingFetchXml();
            RetrieveMultipleRecordsUsingFetchXmlWithColumnComparison();
            RetrieveMultipleRecordsUsingFetchXmlWithLinkEntity();
            RetrieveRecentLeadsNoPhoneCall();
        }

        public EntityCollection RetrieveMultipleRecordsUsingFetchXml()
        {
            var query = @"
            <fetch mapping='logical'>
                <entity name='account'>
                    <attribute name='accountid'/>
                    <attribute name='name'/>
                    <filter type='and'>
                        <condition attribute='statecode' operator='eq' value='0'/>
                        <condition attribute='donotemail' operator='eq' value='false'/>
                        <condition attribute='name' operator='not-null'/>
                        <condition attribute='createdon' operator='this-year'/>
                    </filter>
                    <order attribute='createdon' descending='true'/>
                </entity>
            </fetch> 
            ";

            var result = crmServiceClient.RetrieveMultiple(new FetchExpression(query));
            result.PrintEntityCollection();

            return result;
        }

        public EntityCollection RetrieveMultipleRecordsUsingFetchXmlWithColumnComparison()
        {
            var query = @"
            <fetch mapping='logical' top='10'>
                <entity name='systemuser'>
                    <attribute name='fullname' />
                    <filter>
                        <condition attribute='isdisabled' operator='eq' value='false' />
                        <condition attribute='firstname' operator='eq' valueof='lastname' />
                    </filter>
                    <order attribute='createdon' descending='true' />
                </entity>
            </fetch>
            ";

            var result = crmServiceClient.RetrieveMultiple(new FetchExpression(query));
            result.PrintEntityCollection("fullname");

            return result;
        }

        public EntityCollection RetrieveMultipleRecordsUsingFetchXmlWithLinkEntity()
        {
            var query = @"
            <fetch mapping='logical' top='10'>
                <entity name='contact'>
                    <attribute name='fullname'/>
                    <filter>
                        <condition attribute='statecode' operator='eq' value='0'/>
                        <condition attribute='parentcustomerid' operator='not-null'/>
                    </filter>
                    <order attribute='createdon' descending='true'/>
                    <link-entity name='account' alias='parentaccount' to='parentcustomerid' from='accountid' link-type='inner'>
                        <attribute name='name'/>
                        <link-entity name='contact' alias='parentaccount.primarycontact' to='primarycontactid' from='contactid' link-type='inner'>
                            <attribute name='fullname'/>
                        </link-entity>
                    </link-entity>
                </entity>
            </fetch> 
            ";

            var result = crmServiceClient.RetrieveMultiple(new FetchExpression(query));
            foreach (var entity in result.Entities)
                entity.PrintAttributes();

            return result;
        }

        public EntityCollection RetrieveRecentLeadsNoPhoneCall()
        {
            var query = @"
            <fetch mapping='logical' top='5'>
                <entity name='lead'>
                    <attribute name='subject'/>
                    <filter>
                        <condition attribute='statecode' operator='eq' value='0'/>
                    </filter>
                    <order attribute='createdon' descending='true'/>
                    <link-entity name='phonecall' alias='phonecall' to='leadid' from='regardingobjectid' link-type='outer'>
                        <filter>
                            <condition attribute='activityid' operator='null'/>
                        </filter>
                    </link-entity>
                </entity>
            </fetch> 
            ";

            var result = crmServiceClient.RetrieveMultiple(new FetchExpression(query));
            foreach (var entity in result.Entities)
                entity.PrintAttributes();

            return result;
        }
    }
}