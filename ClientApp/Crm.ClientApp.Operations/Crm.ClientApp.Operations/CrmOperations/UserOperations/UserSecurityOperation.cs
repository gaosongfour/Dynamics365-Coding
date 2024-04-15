using System;
using System.Collections;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System.Text;
using Crm.Common;

namespace Crm.ClientApp.Operations.CrmOperations
{
    public class UserSecurityOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            var userId = WhoAmI();
            RetrieveUserTeams(userId);
            RetrieveUserRoles(userId);

            var roleNameArray = new string[] { "System Administrator" };
            var adminUsers = RetrieveActiveUsersByRoleName(roleNameArray);
        }

        #region Retreive user roles and teams
        /// <summary>
        /// Get security roles for specific user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public EntityCollection RetrieveUserRoles(Guid userId)
        {
            var query = new QueryExpression();
            query.EntityName = "role";
            query.ColumnSet = new ColumnSet("name");

            var linkEntity = new LinkEntity("role", "systemuserroles", "roleid", "roleid", JoinOperator.Inner);
            linkEntity.LinkCriteria = new FilterExpression();
            linkEntity.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);
            query.LinkEntities.Add(linkEntity);

            var result = crmServiceClient.RetrieveMultiple(query);

            Console.WriteLine($"User with id {userId} has {result.Entities.Count} security roles");

            foreach (var roleEntity in result.Entities)
                Console.WriteLine(roleEntity.GetAttributeValue<string>("name"));

            return result;
        }

        /// <summary>
        /// Get teams the user belongs to
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public EntityCollection RetrieveUserTeams(Guid userId)
        {
            var query = new QueryExpression();
            query.EntityName = "team";
            query.ColumnSet = new ColumnSet("name", "isdefault", "teamtype");

            var linkEntity = new LinkEntity("team", "teammembership", "teamid", "teamid", JoinOperator.Inner);
            linkEntity.LinkCriteria = new FilterExpression();
            linkEntity.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);
            query.LinkEntities.Add(linkEntity);

            var result = crmServiceClient.RetrieveMultiple(query);

            Console.WriteLine($"User with id {userId} is member of  {result.Entities.Count} teams");

            foreach (var entity in result.Entities)
            {
                var name = entity.GetAttributeValue<string>("name");
                var isDefault = entity.FormattedValues["isdefault"];
                var type = entity.FormattedValues["teamtype"];
                Console.WriteLine($"Team name=>{name};Is Default=>{isDefault};Team type=>{type}");
            }

            return result;
        }
        #endregion

        #region Retrieve User list by roles
        public EntityCollection RetrieveActiveUsersByRoleName(string[] roleNameArray)
        {
            if (roleNameArray?.Length == 0)
                throw new ArgumentException("roleNameArray should not be null or empty");

            var roleNameValueString = new StringBuilder();
            foreach (string roleName in roleNameArray)
            {
                roleNameValueString.AppendLine($"<value>{roleName}</value>");
            }

            var fetchXml = $@"
            <fetch mapping='logical'>
              <entity name='systemuser'>
                <attribute name='domainname'/>
                <attribute name='fullname'/>
                <filter>
                  <condition attribute='isdisabled' operator='eq' value='false'/>
                </filter>
                <link-entity name='systemuserroles' alias='ur' to='systemuserid' from='systemuserid' link-type='inner'>
                  <link-entity name='role' alias='r' to='roleid' from='roleid' link-type='inner'>
                    <attribute name='name'/>
                    <filter>
                      <condition attribute='name' operator='in'>
                       {roleNameValueString}
                      </condition>
                    </filter>
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>
            ";

            var result = crmServiceClient.RetrieveMultiple(new FetchExpression(fetchXml));

            foreach (var entity in result.Entities)
            {
                var fullname = entity.GetAttributeValue<string>("fullname");
                var roleName = entity.GetAliasedAttributeValue<string>("r.name");
                Console.WriteLine($"User=>{fullname};Role=>{roleName}");
            }

            return result;
        }
        #endregion
    }
}