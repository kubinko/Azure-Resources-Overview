using AzureFunctionsDemo.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace AzureFunctionsDemo.Functions
{
    public static class HttpTriggerFunctions
    {
        [FunctionName(nameof(HelloWorld))]
        public static async Task<IActionResult> HelloWorld(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        [FunctionName(nameof(HelloUser))]
        public static IActionResult HelloUser(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Function-authorized endpoint accessed.");
            return new OkObjectResult("Hello from function-authorized endpoint.");
        }

        [FunctionName(nameof(HelloAdmin))]
        public static IActionResult HelloAdmin(
            [HttpTrigger(AuthorizationLevel.Admin, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Admin-authorized endpoint accessed.");
            return new OkObjectResult("Hello from admin-authorized endpoint.");
        }

        [FunctionName(nameof(CustomPayload))]
        public static IActionResult CustomPayload(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] Person person,
            ILogger log)
        {
            if (person.Id == 0)
            {
                return new BadRequestObjectResult("Invalid payload.");
            }
            else
            {
                return new OkObjectResult(person.ToString());
            }
        }

        [FunctionName(nameof(CustomRoute))]
        public static IActionResult CustomRoute(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{id:int}")] HttpRequest req,
            int id,
            ILogger log)
        {
            return new OkObjectResult($"Order detail - {id}.");
        }
    }
}
