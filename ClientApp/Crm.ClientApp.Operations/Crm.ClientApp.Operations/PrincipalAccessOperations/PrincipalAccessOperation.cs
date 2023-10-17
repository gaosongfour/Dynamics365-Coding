using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Crm.ClientApp.Operations.Helpers;

namespace Crm.ClientApp.Operations.PrincipalAccessOperations
{
    public class PrincipalAccessOperationn : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            var userId = ConsoleHelper.GetGuidUserInput("Please provide Guid formated user Id");
            var userEntity = crmServiceClient.Retrieve("systemuser", userId, new ColumnSet("domainname"));
            Console.WriteLine($"user {userEntity.GetAttributeValue<string>("domainname")} retrieved");

            var accountEntity = RetrieveLatestAccount();
            if (accountEntity == null)
            {
                Console.WriteLine("0 account retrieved, app stopped");
                return;
            }
            Console.WriteLine($"account {accountEntity.GetAttributeValue<string>("name")} retrieved");

            var accountEntityRef = accountEntity.ToEntityReference();
            var userEntityRef = userEntity.ToEntityReference();
            
            RetrievePrincipalAccessRight(accountEntityRef, userEntityRef);
            RetrieveShareAccessRight(accountEntityRef);
            //Share to user WriteAccess and DeleteAccess, 
            //Strange that on Web UI side, Delete option is locked if the user has no delete basic privilege on it, but below code can be still executed and Delete privilege is "shared".
            ShareRecord(accountEntityRef, userEntityRef);
            RetrieveShareAccessRight(accountEntityRef);

            ModifyShareAccessRight(accountEntityRef, userEntityRef);
            RetrieveShareAccessRight(accountEntityRef);

            UnshareRecord(accountEntityRef, userEntityRef);
            RetrieveShareAccessRight(accountEntityRef);
        }

        /// <summary>
        /// Retrieve the access right of user/team for specified record
        /// </summary>
        /// <param name="entityRef"></param>
        /// <param name="principalEntityRef"></param>
        public void RetrievePrincipalAccessRight(EntityReference entityRef, EntityReference principalEntityRef)
        {
            var request = new RetrievePrincipalAccessRequest
            {
                Principal = principalEntityRef,
                Target = entityRef
            };

            var response = (RetrievePrincipalAccessResponse)crmServiceClient.Execute(request);
            Console.WriteLine($"current access rights=>{response.AccessRights}");
        }

        public void RetrieveShareAccessRight(EntityReference entityRef)
        {
            var request = new RetrieveSharedPrincipalsAndAccessRequest
            {
                Target = entityRef
            };

            var response = (RetrieveSharedPrincipalsAndAccessResponse)crmServiceClient.Execute(request);
            Console.WriteLine($"Shared access right count=>{response.PrincipalAccesses.Length}");
            foreach (var principalAccess in response.PrincipalAccesses)
            {
                Console.WriteLine($"{principalAccess.Principal.LogicalName}-{principalAccess.Principal.Id}=>{principalAccess.AccessMask}");
            }
        }

        /// <summary>
        /// Share a record on a speficied record
        /// </summary>
        /// <param name="entityRef"></param>
        /// <param name="principalEntityRef"></param>
        /// <param name="accessRights"></param>
        public void ShareRecord(EntityReference entityRef, EntityReference principalEntityRef, AccessRights accessRights = AccessRights.WriteAccess | AccessRights.DeleteAccess)
        {
            var request = new GrantAccessRequest
            {
                PrincipalAccess = new PrincipalAccess
                {
                    AccessMask = accessRights,
                    Principal = principalEntityRef
                },
                Target = entityRef
            };

            var response = (GrantAccessResponse)crmServiceClient.Execute(request);
            Console.WriteLine($"Access rights granted=>{accessRights}");
        }

        /// <summary>
        /// Update the sharing access right on a speficied record
        /// </summary>
        /// <param name="entityRef"></param>
        /// <param name="principalEntityRef"></param>
        /// <param name="accessRights"></param>
        public void ModifyShareAccessRight(EntityReference entityRef, EntityReference principalEntityRef, AccessRights accessRights = AccessRights.ReadAccess | AccessRights.WriteAccess)
        {
            var request = new ModifyAccessRequest
            {
                PrincipalAccess = new PrincipalAccess
                {
                    AccessMask = accessRights,
                    Principal = principalEntityRef
                },
                Target = entityRef
            };
            var response = (ModifyAccessResponse)crmServiceClient.Execute(request);
            Console.WriteLine($"Access rights modified=>{accessRights}");
        }

        /// <summary>
        /// Revoke share access right for a principal from a specified record
        /// </summary>
        /// <param name="entityRef"></param>
        /// <param name="principalEntityRef"></param>
        public void UnshareRecord(EntityReference entityRef, EntityReference principalEntityRef)
        {
            var request = new RevokeAccessRequest
            {
                Target = entityRef,
                Revokee = principalEntityRef
            };
            var response = (RevokeAccessResponse)crmServiceClient.Execute(request);
            Console.WriteLine("Access Rights Revoked");
        }
    }
}
