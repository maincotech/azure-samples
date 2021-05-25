using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Maincotech.FunctionSamples
{
    public static class DurableFunctions
    {
        [FunctionName("Function1")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("Function1_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }


        [FunctionName("UpdateCounter")]
        public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
    [DurableClient] IDurableEntityClient entityClient,
    ILogger log)
        {
            log.LogInformation("start counter operations.");

            string operation = req.Query["op"]; // operation add, reset,get

            if (string.IsNullOrWhiteSpace(operation))
            {
                return new BadRequestObjectResult("The operation must be specified.");
            }

            operation = operation.ToLowerInvariant();
            var entityId = new EntityId("Counter", "operations");
            switch (operation)
            {
                case "add":
                    await entityClient.SignalEntityAsync(entityId, "add", 1);
                    break;
                case "reset":
                    await entityClient.SignalEntityAsync(entityId, "reset");
                    break;
                case "get":
                   var result =  await entityClient.ReadEntityStateAsync<Counter>(entityId);
                    if (result.EntityExists)
                    {
                        return new OkObjectResult($"The counter's value is {result.EntityState.CurrentValue}");
                    }
                    return new NotFoundObjectResult("Can find the counter");
                default:
                    return new BadRequestObjectResult($"The specified operation '{operation}' is not supported.");
            }

            return new OkObjectResult("The operation has been executed successfully.");
        }
    }
}