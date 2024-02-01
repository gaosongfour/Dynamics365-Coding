using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;

namespace Crm.ClientApp.Operations.CrmOperations
{
    /// <summary>
    /// Use ExecuteTransactionRequest
    /// </summary>
    public class ExecuteTransactionOperation : CrmOperationBase, ICrmOperation
    {
        /// <summary>
        /// Create Order entity and Order Items in the single Transaction
        /// </summary>
        public void Execute()
        {
            var request = new ExecuteTransactionRequest()
            {
                Requests = new OrganizationRequestCollection(),
                ReturnResponses = true
            };

            request.Requests.AddRange(GetCreateOrderRequest());

            try
            {
                var response = (ExecuteTransactionResponse)crmServiceClient.Execute(request);

                foreach (var responseItem in response.Responses)
                {
                    Console.WriteLine($"{ ((CreateResponse)responseItem).id}");
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                var transactionFault = (ExecuteTransactionFault)ex.Detail;
                Console.WriteLine($"ExecuteTransactionFault occured=>{transactionFault.FaultedRequestIndex + 1}:{ex.Detail.Message}");
                throw ex;
            }
        }

        private List<OrganizationRequest> GetCreateOrderRequest()
        {
            var clientEntity = RetrieveLastestClient();
            if (clientEntity == null) throw new Exception("Retrieve client failed");

            var factoryEntity = RetrieveLastestFactory();
            if (factoryEntity == null) throw new Exception("Retrieve factory failed");

            var requests = new List<OrganizationRequest>();

            var orderEntity = new Entity("new_order");
            orderEntity.Id = Guid.NewGuid();
            orderEntity["new_clientid"] = clientEntity.ToEntityReference();
            orderEntity["new_productionfactoryid"] = factoryEntity.ToEntityReference();

            var createOrderRequest = new CreateRequest() { Target = orderEntity };
            requests.Add(createOrderRequest);

            //add order item create request, the total amount will be 6000 exceeding the limit 5000, the plugin will prevent the creation so this transaction will roll back 
            requests.Add(GetCreateOrderItemRequest(orderEntity.Id, 3000));
            requests.Add(GetCreateOrderItemRequest(orderEntity.Id, 2000));
            requests.Add(GetCreateOrderItemRequest(orderEntity.Id, 1000));

            return requests;
        }

        private CreateRequest GetCreateOrderItemRequest(Guid orderId, decimal orderItemAmount)
        {
            var orderItemEntity = new Entity("new_orderitem");
            orderItemEntity["new_orderid"] = new EntityReference("new_order", orderId);
            orderItemEntity["new_price"] = orderItemEntity["new_amount"] = new Money(orderItemAmount);
            orderItemEntity["new_quantity"] = 1m;

            return new CreateRequest() { Target = orderItemEntity };
        }
    }
}
