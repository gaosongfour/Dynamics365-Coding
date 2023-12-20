using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Crm.Plugin.Business.EntityManagement.Order
{
    /// <summary>
    /// Plugin execute on entity [new_orderitem], event pre-create, sync mode
    /// It checks if the total amount will exceed the limit
    /// </summary>
    public class OrderMaxAmountCheck : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context.PrimaryEntityName != "new_orderitem")
                throw new InvalidPluginExecutionException("Plugin should be registered on entity [new_orderitem]");

            if (context.MessageName.ToLower() != "create")
                throw new InvalidPluginExecutionException("Plugin should be registered on Create message");

            var targetEntity = (Entity)context.InputParameters["Target"];

            if (!targetEntity.Contains("new_orderid"))
                throw new InvalidPluginExecutionException("Order is required for Order Item");

            if (!targetEntity.Contains("new_amount"))
                throw new InvalidPluginExecutionException("Amount is required for Order Item");

            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("start checking");
            var orderEntityRef = targetEntity.GetAttributeValue<EntityReference>("new_orderid");
            tracingService.Trace($"orderEntityRef=>{orderEntityRef?.Id}");
            var currentTotalAmount = GetOrderTotalAmount(service, orderEntityRef.Id);
            tracingService.Trace($"currentTotalAmount=>{currentTotalAmount}");
            var orderItemAmount = targetEntity.GetAttributeValue<Money>("new_amount").Value;
            tracingService.Trace($"orderItemAmount=>{targetEntity.Contains("new_amount")}");
            var max = GetOrderMaxAmount(service);

            if (currentTotalAmount + orderItemAmount > max)
                throw new InvalidPluginExecutionException($"Order Item creation failed, current total amount ({currentTotalAmount + orderItemAmount}) execeeds the limit amount({max})");
        }

        private decimal GetOrderTotalAmount(IOrganizationService service, Guid orderId)
        {
            var query = $@"
            <fetch distinct='false' mapping='logical' aggregate='true'>
               <entity name='new_orderitem'>
                  <attribute name='new_amount' aggregate='sum' alias='totalAmount'/>
                  <filter>
                     <condition attribute='new_orderid' operator='eq' value='{orderId}'/>
                  </filter>
               </entity>
            </fetch>
            ";

            var result = service.RetrieveMultiple(new FetchExpression(query));
            var totalAmount = 0m;
            if (result.Entities[0].Contains("totalAmount"))
            {
                var aliasedValue = (Money)result.Entities[0].GetAttributeValue<AliasedValue>("totalAmount").Value;
                if (aliasedValue != null)
                    totalAmount = aliasedValue.Value;
            }
            return totalAmount;
        }

        /// <summary>
        /// Fack method, to get a max total amount of this order. 
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        private decimal GetOrderMaxAmount(IOrganizationService service)
        {
            return 5000m;
        }
    }
}
