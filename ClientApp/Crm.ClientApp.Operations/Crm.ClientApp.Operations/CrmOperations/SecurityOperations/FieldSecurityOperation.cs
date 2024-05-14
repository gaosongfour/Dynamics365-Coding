using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Crm.ClientApp.Operations.CrmOperations
{
    /// <summary>
    /// Filed Security (Column Level) operations
    /// </summary>
    public class FieldSecurityOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            var entityName = "contact";
            var attributeName = "telephone2"; //mobile phone
            CreateFieldPermissionForAllProfiles(entityName, attributeName);
        }

        private void CreateFieldPermissionForAllProfiles(string entityName, string attributeName)
        {
            var profileResult = GetAllFieldSecurityProfile();
            var permissionResult = GetFieldSecurityPermissions(entityName, attributeName);
            foreach (var profileEntity in profileResult.Entities)
            {
                var profileName = profileEntity.GetAttributeValue<string>("name");
                try
                {
                    if (permissionResult.Entities.Any(p => p.GetAttributeValue<EntityReference>("fieldsecurityprofileid").Id == profileEntity.Id))
                    {
                        Console.WriteLine($"{profileEntity.Id}|{profileName}=>exist");
                    }
                    else
                    {
                        var permissionId = CreateFieldPermission(profileEntity.ToEntityReference(), entityName, attributeName, true, false, false);
                        Console.WriteLine($"{profileEntity.Id}|{profileName}=>created|{permissionId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{profileEntity.Id}|{profileName}=>ex:{ex.Message}");
                }
            }
        }

        private EntityCollection GetAllFieldSecurityProfile()
        {
            var query = new QueryExpression("fieldsecurityprofile")
            {
                ColumnSet = new ColumnSet("name")
            };

            return crmServiceClient.RetrieveMultiple(query);
        }

        /// <summary>
        /// Retrieve all existing security permission for specific entity and attribute(filed)
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        private EntityCollection GetFieldSecurityPermissions(string entityName, string attributeName)
        {
            var query = new QueryExpression("fieldpermission")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("entityname", ConditionOperator.Equal,entityName),
                        new ConditionExpression("attributelogicalname", ConditionOperator.Equal,attributeName),
                    }
                }
            };

            return crmServiceClient.RetrieveMultiple(query);
        }

        private Guid CreateFieldPermission(EntityReference profileEntityRef, string entityName, string attributeName, bool canRead, bool canCreate, bool canUpdate)
        {
            var entity = new Entity("fieldpermission");
            entity["fieldsecurityprofileid"] = profileEntityRef;
            entity["entityname"] = entityName;
            entity["attributelogicalname"] = attributeName;

            //if field is calculated fileds,  attributes [canCreate] and [canUpdate] are not applicable
            entity["canread"] = setPermissionValue(canRead);
            entity["cancreate"] = setPermissionValue(canCreate);
            entity["canupdate"] = setPermissionValue(canUpdate);

            return crmServiceClient.Create(entity);
        }

        private OptionSetValue setPermissionValue(bool canPerformAction)
        {
            //0=>Not allowed
            //4=>Allowed
            return new OptionSetValue(canPerformAction ? 4 : 0);
        }
    }
}
