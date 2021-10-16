using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;

namespace AzureFunctionsDemo.Functions
{
    public static class TimerTriggerFunctions
    {
        [FunctionName(nameof(TimerTriggerFunction))]
        public static void TimerTriggerFunction([TimerTrigger("%TimerSchedule%")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            log.LogInformation($"C# Timer trigger is running {(myTimer.IsPastDue ? "late" : "on time")}.");
            log.LogInformation($"Next 5 occurences:\n{myTimer.FormatNextOccurrences(5)}");
        }
    }
}
