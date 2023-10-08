using System;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace Crm.ClientApp.Operations.UserOperations
{
    public class UserSecurityOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            var userId = WhoAmI();
            RetrieveUserTeams(userId);
            RetrieveUserRoles(userId);
        }

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
    }
}