using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DurableFunctionsDemo
{
    internal class AccountClass
    {
        [JsonPropertyName("value")]
        public double CurrentValue { get; set; }

        public void Credit(double amount) => CurrentValue += amount;

        public void Debit(double amount) => CurrentValue -= amount;

        public void Reset() => CurrentValue = 0;

        public double Get() => CurrentValue;

        [FunctionName(nameof(AccountClass))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<AccountClass>();
    }
}
