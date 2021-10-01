using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureFunctionsDemo.Functions
{
    public static class BlobStorageFunctions
    {
        [FunctionName(nameof(ProcessNewBlob))]
        public static void ProcessNewBlob(
            [BlobTrigger("file-upload/{name}", Connection = "BlobStorageConnectionString")] Stream myBlob,
            string name,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }

        [FunctionName(nameof(FindBlob))]
        public static IActionResult FindBlob(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "blobs/{id}")] HttpRequest req,
            [Blob("catalog/{id}", FileAccess.ReadWrite, Connection = "BlobStorageConnectionString")] CloudBlockBlob myBlob,
            ILogger log)
        {
            if (myBlob.Properties.ETag == null)
            {
                return new NotFoundObjectResult(myBlob.Name);
            }

            return new OkObjectResult($"Blob {myBlob.Name} found.\n" +
                $"Size:         {myBlob.Properties.Length:###,###,##0 B}\n" +
                $"Content Type: {myBlob.Properties.ContentType}");
        }

        [FunctionName(nameof(ProcessFile))]
        public static async Task ProcessFile(
            [BlobTrigger("images/{name}", Connection = "BlobStorageConnectionString")] CloudBlockBlob rawBlob,
            [Blob("images-processed/{name}-report", FileAccess.ReadWrite, Connection = "BlobStorageConnectionString")] CloudBlockBlob processedBlob,
            ILogger log)
        {
            log.LogInformation($"Incoming blob\n Name:{rawBlob.Name} \n Size: {rawBlob.Properties.Length} Bytes");

            await processedBlob.UploadTextAsync($"Blob processed at {DateTime.Now}.");
            log.LogInformation($"Incoming blob processed and moved.");

            await rawBlob.DeleteIfExistsAsync();
            log.LogInformation($"Original blob deleted.");
        }
    }
}
