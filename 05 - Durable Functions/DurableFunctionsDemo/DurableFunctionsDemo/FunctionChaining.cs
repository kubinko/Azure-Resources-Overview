using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableFunctionsDemo
{
    public static class FunctionChaining
    {
        [FunctionName(nameof(HttpStart))]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string repository = await req.ReadAsStringAsync();
            string instanceId = await starter.StartNewAsync<string>(nameof(RunOrchestrator), repository);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(RunOrchestrator))]
        public static async Task<Result> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var repository = context.GetInput<string>();
            var result = new Result();

            DateTime startTime = context.CurrentUtcDateTime;
            result.BuildResult = await context.CallActivityAsync<Status>(nameof(Build), repository);
            result.BuildDuration = context.CurrentUtcDateTime - startTime;
            result.TestsPassed = await context.CallActivityAsync<int>(nameof(Test), repository);
            result.Artifacts = await context.CallActivityAsync<Artifact[]>(nameof(Publish), repository);
            result.DeployTime = await context.CallActivityAsync<DateTimeOffset>(nameof(Deploy), result.Artifacts);

            return result;
        }

        [FunctionName(nameof(Build))]
        public static Status Build([ActivityTrigger] string repository, ILogger log)
        {
            log.LogInformation($"Building solution in repository {repository}.");
            return Status.Success;
        }

        [FunctionName(nameof(Test))]
        public static int Test([ActivityTrigger] string repository, ILogger log)
        {
            log.LogInformation($"Testing solution in repository {repository}.");
            return new Random().Next(1, 100);
        }

        [FunctionName(nameof(Publish))]
        public static Artifact[] Publish([ActivityTrigger] string repository, ILogger log)
        {
            log.LogInformation($"Publishing solution in repository {repository}.");
            return new[] { new Artifact("Project"), new Artifact("Client") };
        }

        [FunctionName(nameof(Deploy))]
        public static DateTimeOffset Deploy([ActivityTrigger] IEnumerable<Artifact> artifacts, ILogger log)
        {
            foreach (var artifact in artifacts)
            {
                log.LogInformation($"Deploying {artifact}.");
            }
            log.LogInformation($"Deployment finished");

            return DateTimeOffset.UtcNow;
        }
    }
}