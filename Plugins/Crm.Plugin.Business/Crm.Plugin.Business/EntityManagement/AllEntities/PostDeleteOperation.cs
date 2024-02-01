using Microsoft.Xrm.Sdk;
using System;

namespace Crm.Plugin.Business.EntityManagement.AllEntities
{
    public class PostDeleteOperation : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.MessageName.ToLower() != "delete")
                throw new InvalidPluginExecutionException("Plugin should be registered on Delete message");

            if (context.Stage != 40)
                throw new InvalidPluginExecutionException("Plugin should be registered on Post-Event");

            //In case of a lead record is deleted, we get the deleted lead inforamtion from SharedVariable of PreDelete Event and processing it
            if (context.PrimaryEntityName == "lead")
            {
                if (context.SharedVariables.Contains("DeleteLeadInfo"))
                {
                    var info = context.SharedVariables["DeleteLeadInfo"]?.ToString();
                    tracingService.Trace(info); // we could write into a dedicated log entity in CRM
                }
                else
                {
                    tracingService.Trace("DeleteLeadInfo not found in SharedVariables");
                }
            }
        }
    }
}
