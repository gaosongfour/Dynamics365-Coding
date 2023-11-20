using Microsoft.Xrm.Sdk;
using System;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;

namespace Crm.Common.Concurrency
{
    public static class CrmConcurrencyExtension
    {
        /// <summary>
        /// Update Record using OptimisticConcurrency, check if RowVersionMatch
        /// </summary>
        /// <param name="service"></param>
        /// <param name="updateEntity">Entity to be updated with RowVersion Info</param>
        public static void UpdateRecordIfRowVersion(this IOrganizationService service, Entity updateEntity)
        {
            if (string.IsNullOrEmpty(updateEntity.RowVersion))
                throw new ArgumentNullException("RowVersion is not provided for the entity to be updated");

            var request = new UpdateRequest
            {
                Target = updateEntity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            try
            {
                service.Execute(request);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                switch (ex.Detail.ErrorCode)
                {
                    case -2147088254:
                        throw new InvalidOperationException($"Operation Failed, {updateEntity.LogicalName} with id {updateEntity.Id} was updated by others");
                    case -2147088253:
                        throw new InvalidOperationException($"Operation Failed, OptimisticConcurrency Not Enabled for {updateEntity.LogicalName}");
                    default: throw ex;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update Failed=>${ex.Message}");
            }
        }
    }
}
