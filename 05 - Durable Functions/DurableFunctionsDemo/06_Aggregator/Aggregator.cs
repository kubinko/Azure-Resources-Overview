using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DurableFunctionsDemo
{
    public static class Aggregator
    {
        [FunctionName(nameof(PerformTransactionWithEntityFunction))]
        public static async Task<HttpResponseMessage> PerformTransactionWithEntityFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var transaction = JsonSerializer.Deserialize<Transaction>(
                await req.Content.ReadAsStringAsync(),
                new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            string instanceId = await starter.StartNewAsync(nameof(RunOrchestratorForEntityFunction), transaction);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId, true);
        }

        [FunctionName(nameof(RunOrchestratorForEntityFunction))]
        public static async Task<double> RunOrchestratorForEntityFunction(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var transaction = context.GetInput<Transaction>();
            var entityId = new EntityId(nameof(Account), "myAccount");
            await context.CallEntityAsync<double>(entityId, transaction.Operation, transaction.Amount);
            var currentAmount = await context.CallEntityAsync<double>(entityId, "get");

            return currentAmount;
        }

        [FunctionName(nameof(Account))]
        public static async Task Account([EntityTrigger] IDurableEntityContext ctx)
        {
            double currentValue = ctx.GetState<double>();

            switch (ctx.OperationName)
            {
                case "credit":
                    double amountC = ctx.GetInput<double>();
                    currentValue += amountC;
                    break;

                case "debit":
                    double amountD = ctx.GetInput<double>();
                    currentValue -= amountD;
                    break;

                case "get":
                    ctx.Return(currentValue);
                    return;
            }

            ctx.SetState(currentValue);
        }

        [FunctionName(nameof(PerformTransactionWithEntityClass))]
        public static async Task<HttpResponseMessage> PerformTransactionWithEntityClass(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var transaction = JsonSerializer.Deserialize<Transaction>(
                await req.Content.ReadAsStringAsync(),
                new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            string instanceId = await starter.StartNewAsync(nameof(RunOrchestratorForEntityClass), transaction);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId, true);
        }

        [FunctionName(nameof(RunOrchestratorForEntityClass))]
        public static async Task<double> RunOrchestratorForEntityClass(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var transaction = context.GetInput<Transaction>();
            var entityId = new EntityId(nameof(AccountClass), "myClassAccount");
            context.SignalEntity(entityId, transaction.Operation, transaction.Amount);

            var currentAmount = await context.CallEntityAsync<double>(entityId, "get");

            return currentAmount;
        }
    }
}