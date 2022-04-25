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
    public static class Monitor
    {
        [FunctionName(nameof(StartMonitor))]
        public static async Task<HttpResponseMessage> StartMonitor(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(req.RequestUri.Query);
            var location = queryDictionary["location"];

            string instanceId = $"{location}Monitor";
            await starter.StartNewAsync(nameof(RunOrchestrator), instanceId, location);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(RunOrchestrator))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            string location = context.GetInput<string>();

            int temperature = await context.CallActivityAsync<int>(nameof(CheckTemperature), location);

            await context.CallActivityAsync(nameof(Log), $"Temperature: {temperature}°C");

            if (temperature < 10)
            {
                DateTime timeOfNextCheck = context.CurrentUtcDateTime.AddSeconds(5);
                await context.CreateTimer(timeOfNextCheck, CancellationToken.None);

                context.ContinueAsNew(location);
            }
            else
            {
                await context.CallActivityAsync(nameof(Log), "Target temperature achieved, ending.");
            }
        }

        [FunctionName(nameof(CheckTemperature))]
        public static int CheckTemperature([ActivityTrigger] string location)
            => new Random().Next(0, 20);

        [FunctionName(nameof(Log))]
        public static void Log([ActivityTrigger] string message, ILogger log)
            => log.LogInformation(message);
    }
}