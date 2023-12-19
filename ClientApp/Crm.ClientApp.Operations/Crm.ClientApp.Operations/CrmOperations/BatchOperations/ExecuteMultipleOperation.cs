using System;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;

namespace Crm.ClientApp.Operations.CrmOperations
{
    /// <summary>
    /// Use of ExecuteMultipleRequest
    /// </summary>
    public class ExecuteMultipleOperation : CrmOperationBase, ICrmOperation
    {
        private readonly int requestNumber = 100;

        public void Execute()
        {
            var watch = new Stopwatch();

            watch.Start();
            RunExecuteMultipleRequest();
            watch.Stop();
            Console.WriteLine($"RunExecuteMultipleRequest=>{watch.ElapsedMilliseconds}");

            watch.Restart();
            RunLoopSingleRequest();
            watch.Stop();
            Console.WriteLine($"RunLoopSingleRequest=>{watch.ElapsedMilliseconds}");
        }

        private void RunExecuteMultipleRequest()
        {
            var request = new ExecuteMultipleRequest();
            request.Settings = new ExecuteMultipleSettings() { ReturnResponses = true, ContinueOnError = true };
            request.Requests = new OrganizationRequestCollection();

            for (int i = 1; i <= requestNumber; i++)
            {
                request.Requests.Add(GetCreateLeadRequest($"Lead-ExeMultiple-{i}"));
            }

            var result = (ExecuteMultipleResponse)crmServiceClient.Execute(request);

            foreach (var response in result.Responses)
            {
                if (response.Fault != null)
                {
                    Console.WriteLine($"{response.RequestIndex} error=>{response.Fault.Message}");
                    continue;
                }

                if (response.Response.ResponseName == "Create")
                {
                    Console.WriteLine($"{ ((CreateResponse)response.Response).id}");
                }
                else
                {
                    Console.WriteLine($"{response.Response.ResponseName}");
                }
            }
        }

        private void RunLoopSingleRequest()
        {
            for (var i = 1; i <= requestNumber; i++)
            {
                try
                {
                    var response = (CreateResponse)crmServiceClient.Execute(GetCreateLeadRequest($"Lead-LoopCreate-{i}"));
                    Console.WriteLine($"{i} created with id {response.id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{i} error=>{ex.Message}");
                }
            }
        }

        private OrganizationRequest GetCreateLeadRequest(string leadName)
        {
            var entity = new Entity("lead");
            entity["subject"] = leadName;
            return new CreateRequest()
            {
                Target = entity
            };
        }
    }
}
