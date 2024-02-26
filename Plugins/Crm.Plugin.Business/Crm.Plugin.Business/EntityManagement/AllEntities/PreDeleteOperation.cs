using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Crm.Plugin.Business.EntityManagement.AllEntities
{
    public class PreDeleteOperation : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context.MessageName.ToLower() != "delete")
                throw new InvalidPluginExecutionException("Plugin should be registered on Delete message");
            if (context.Stage != 20)
                throw new InvalidPluginExecutionException("Plugin should be registered on Pre-Event");

            if (context.PrimaryEntityName == "lead")
            {
                PreDeleteLeadValidation(context, tracingService);
                //Get the required lead information, then passed into shared variable
                var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                var service = serviceFactory.CreateOrganizationService(context.UserId);
                var leadEntity = service.Retrieve("lead", context.PrimaryEntityId, new ColumnSet("subject"));
                var leadName = leadEntity.GetAttributeValue<string>("subject");
                context.SharedVariables["DeleteLeadInfo"] = $"The lead record with Id {leadEntity.Id} and name [{leadName}] was deleted";
                tracingService.Trace("SharedVariables[DeleteLeadInfo] set");
            }
        }

        private void PreDeleteLeadValidation(IPluginExecutionContext context, ITracingService tracingService)
        {
            tracingService.Trace($"PreDeleteLeadValidation=>{context.PrimaryEntityId}");
            //do some validation check here, if the lead entity is not allowed to be deleted, throw InvalidPluginExecutionException
        }
    }
}
