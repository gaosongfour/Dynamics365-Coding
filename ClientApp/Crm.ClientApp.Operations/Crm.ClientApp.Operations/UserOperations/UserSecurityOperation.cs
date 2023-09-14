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
            RetrieveUserRoles(userId);
        }

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
    }
}