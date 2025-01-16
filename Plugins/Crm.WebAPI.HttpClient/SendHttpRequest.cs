using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using System;
using System.Net.Http;

namespace Crm.WebAPI.HttpClientDemo
{
    public class SendHttpRequest : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (!context.InputParameters.Contains("new_crmsendrequest_requesturi"))
                throw new InvalidPluginExecutionException("missing input parameters new_crmsendrequest_requesturi");

            var uri = context.InputParameters["new_crmsendrequest_requesturi"].ToString();
            tracingService.Trace($"uri=>{uri}");

            try
            {
                var responseText = SendRequest(uri);
                tracingService.Trace($"response=>{responseText}");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"ex=>{ex.Message}");
            }
        }

        private string SendRequest(string uri)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.ConnectionClose = true; //Set KeepAlive to false

            var responseBody = client.GetStringAsync(uri).GetAwaiter().GetResult();
            return responseBody;
        }
    }
}
