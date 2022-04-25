using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctionsDemo
{
    public static class AsyncHttpApi
    {
        [FunctionName(nameof(HttpStart))]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var shouldFail = bool.Parse(await req.Content.ReadAsStringAsync());

            string instanceId = await starter.StartNewAsync(nameof(RunOrchestrator), Guid.NewGuid().ToString(), shouldFail);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId, true);
        }

        [FunctionName(nameof(RunOrchestrator))]
        public static async Task<double> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var shouldFail = context.GetInput<bool>();

            await context.CallActivityAsync(nameof(Log), "Timer start.");

            await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(10), CancellationToken.None);

            await context.CallActivityAsync(nameof(Log), "Timer stop.");

            if (shouldFail)
            {
                throw new Exception($"FATAL ERROR occured during orchestration with instance ID {context.InstanceId}.");
            }
            else
            {
                return new Random().Next(1, 1000);
            }
        }

        [FunctionName(nameof(Log))]
        public static void Log([ActivityTrigger] string message, ILogger log)
            => log.LogInformation(message);
    }
}