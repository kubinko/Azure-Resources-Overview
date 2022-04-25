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
    public static class HumanInteraction
    {
        [FunctionName(nameof(StartVerification))]
        public static async Task<HttpResponseMessage> StartVerification(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync(nameof(RunOrchestrator));

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId, true);
        }

        [FunctionName(nameof(RunOrchestrator))]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            using var cancelTokenSource = new CancellationTokenSource();

            DateTime expiration = context.CurrentUtcDateTime.AddSeconds(20);
            Task timeout = context.CreateTimer(expiration, cancelTokenSource.Token);

            Task<string> approval = context.WaitForExternalEvent<string>("Approve");

            Task winner = await Task.WhenAny(timeout, approval);
            if (winner == approval)
            {
                cancelTokenSource.Cancel();
                return $"Approved by {approval.Result}.";
            }
            else
            {
                return "Expired.";
            }
        }
    }
}