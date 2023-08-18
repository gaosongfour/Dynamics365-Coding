using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace Crm.Plugin.AutoNumber
{
    /// <summary>
    /// Plugin for entity auto number, by autonumber config entity
    /// </summary>
    public class EntityAutoNubmer : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.MessageName != "Create" || context.Stage != 20)
                throw new InvalidPluginExecutionException("Plugin AutoNumber should be registed on pre-create event, please check");

            var targetEntity = (Entity)context.InputParameters["Target"];

            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                switch (targetEntity.LogicalName)
                {
                    case "new_order":
                        var autoNumberService = new AutoNumberService();
                        autoNumberService.SetNumber(service, targetEntity);
                        break;
                    default:
                        throw new InvalidPluginExecutionException($"Unsupported AutoNumber entity {targetEntity.LogicalName}");
                }
            }
            catch (FaultException<IOrganizationService> ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
            catch (Exception ex)
            {
                tracingService.Trace("General Error in plugin EntityAutoNubmer: {0}", ex.Message);
                throw;
            }
        }
    }
}
