using CustomBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsDemo.Functions
{
    public static class CustomBinding
    {
        [FunctionName(nameof(ReadFile))]
        public static IActionResult ReadFile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "custombinding/{name}")] HttpRequest req,
            ILogger log,
            string name,
            [MyFileReaderBinding(Location = "%FilePath%\\{name}")] MyFileReaderModel fileReaderModel)
        {
            return new OkObjectResult(fileReaderModel.Content);
        }
    }
}
