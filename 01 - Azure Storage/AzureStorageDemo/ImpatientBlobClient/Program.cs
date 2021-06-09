using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImpatientBlobClient
{
    class Program
    {
        private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=berthotymeetup;AccountKey=epqRtSN/tPPZgEhuxl2Aq5e2yZv5fmdm1jWOJT0ya7UFgAKcQ+COqTgVJXhp1kUpACO66XnqtXMz395a1wVsLA==;EndpointSuffix=core.windows.net";

        private static BlobServiceClient _client;
        private static BlobContainerClient _container;

        static async Task Main(string[] args)
        {
            _client = new BlobServiceClient(ConnectionString);

            Console.WriteLine($"Initializing blob container 'testcontainer'...");

            _container = _client.GetBlobContainerClient("testcontainer");
            await _container.CreateIfNotExistsAsync(PublicAccessType.None);

            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            await ChangeMetadata();

            Console.ReadKey();
        }

        static async Task ChangeMetadata()
        {
            BlobClient blob = _container.GetBlobClient("imageBlob.jpg");

            Console.WriteLine("Checking blob lease state...");

            for (int i = 0; i <= 10; i++)
            {
                Response<BlobProperties> response = await blob.GetPropertiesAsync();
                if (response.Value.LeaseStatus == LeaseStatus.Unlocked)
                {
                    Console.WriteLine("No lease present, proceeding...");
                    break;
                }
                else
                {
                    Console.WriteLine($"Blob leased. Current lease state: {response.Value.LeaseState}");

                    if (i == 10)
                    {
                        Console.WriteLine("Tired of waiting, breaking lease...");
                        var leaseClient = new BlobLeaseClient(blob);
                        await leaseClient.BreakAsync();
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                }
            }

            Console.WriteLine("Attempting to change blob metadata...");

            var metadata = new Dictionary<string, string>();
            metadata.Add("property_from_another_client", "hello");

            try
            {
                await blob.SetMetadataAsync(metadata);
                Console.WriteLine("Metadata successfully changed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occured:\n{ex.Message}");
            }
        }
    }
}
