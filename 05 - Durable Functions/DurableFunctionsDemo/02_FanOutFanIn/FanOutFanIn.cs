using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctionsDemo
{
    public static class FanOutFanIn
    {
        [FunctionName(nameof(HttpStart))]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var databases = JsonSerializer.Deserialize<string[]>(await req.Content.ReadAsStringAsync());

            string instanceId = await starter.StartNewAsync(nameof(RunOrchestrator), databases);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(RunOrchestrator))]
        public static async Task<double> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var databases = context.GetInput<string[]>();

            var shrinkTasks = databases.Select(db => context.CallActivityAsync<double>(nameof(ShrinkDb), db));
            var shrinkResults = await Task.WhenAll(shrinkTasks);

            double totalFreeSpace = shrinkResults.Sum();
            return totalFreeSpace;
        }

        [FunctionName(nameof(ShrinkDb))]
        public static double ShrinkDb([ActivityTrigger] string database, ILogger log)
        {
            log.LogInformation($"Shrinking database {database}...");
            Thread.Sleep(1000);
            log.LogInformation($"Shrink completed ({database}).");

            return new Random().NextDouble() * 100;
        }
    }
}