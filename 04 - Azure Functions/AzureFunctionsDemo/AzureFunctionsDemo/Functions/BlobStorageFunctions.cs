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

        [FunctionName(nameof(PoisonNewBlob))]
        public static void PoisonNewBlob(
            [BlobTrigger("dangerous/{name}", Connection = "BlobStorageConnectionString")] Stream myBlob,
            string name,
            ILogger log)
        {
            log.LogInformation($"Processing dangerous blob {name}...");
            throw new OperationCanceledException("Something went wrong.");
        }

        [FunctionName(nameof(FindBlob))]
        public static IActionResult FindBlob(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "blobs/{id}")] HttpRequest req,
            [Blob("catalog/{id}", FileAccess.Read, Connection = "BlobStorageConnectionString")] Stream blob,
            string id,
            ILogger log)
        {
            if (blob == null)
            {
                return new NotFoundObjectResult(id);
            }

            return new OkObjectResult($"Blob {id} found.\nSize: {blob.Length:###,###,##0 B}");
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
