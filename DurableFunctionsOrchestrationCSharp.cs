using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public static class DurableFunctionsOrchestrationCSharp
    {
        [FunctionName("DurableFunctionsOrchestrationCSharp")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
        {
            var outputs = new List<string>();
            #region Hide
                
            IEnumerable<X> cities = context.GetInput<IEnumerable<X>>();

            var parallelTasks = new List<Task<string>>();

            foreach (var s in cities)
            {

                var task = context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", s);
                parallelTasks.Add(task);

            }
            // Replace "hello" with the name of your Durable Activity Function.
            await Task.WhenAll(parallelTasks);
            #endregion

            log.LogInformation($"Started orchestration with ID = '{context.InstanceId}'.");

            var result = await context.WaitForExternalEvent<Order>("OrderApprovalResult", TimeSpan.FromHours(1));
            log.LogInformation($"Setting approval status of order for instance {context.InstanceId} with data: {result.Id}");

            await context.CallActivityAsync("DurableFunctionsOrchestrationCSharp_End", null);
            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("ApproveOrderById")]
        public static async Task<IActionResult> ApproveOrderById(
                    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "approve/{id}")]HttpRequest req,
                    [OrchestrationClient] DurableOrchestrationClient client,
                    ILogger log, string id)
        {
            log.LogInformation($"Setting approval status of order {id}");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"Setting approval status of order {requestBody}");

            Order order = Newtonsoft.Json.JsonConvert.DeserializeObject<Order>(requestBody);


            await client.RaiseEventAsync(order.OrchestrationId, "OrderApprovalResult", order);
            return new OkResult();
        }


#region Hide
        [FunctionName("DurableFunctionsOrchestrationCSharp_Hello")]
        public static string SayHello([ActivityTrigger] X name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name.name}.");
            return $"Hello {name.name}!";
        }
        
        [FunctionName("DurableFunctionsOrchestrationCSharp_End")]
        public static string SayBye([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying bye bye");
            return $"Bye bye!";
        }


        [FunctionName("DurableFunctionsOrchestrationCSharp_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("DurableFunctionsOrchestrationCSharp", new[] {
                 new X {name= "London"} ,
                 new X {name= "New York"}
                 });

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
#endregion

   
}