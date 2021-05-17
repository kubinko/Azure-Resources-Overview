using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PatientBlobClient
{
    class Program
    {
        private const string ConnectionString = "<connection string>";

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

            await ChangeMetadataWithoutChecking();
            //await ChangeMetadataWithChecking();

            Console.ReadKey();
        }

        static async Task ChangeMetadataWithoutChecking()
        {
            Console.WriteLine("Attempting to change blob metadata...");

            BlobClient blob = _container.GetBlobClient("imageBlob.jpg");

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

        static async Task ChangeMetadataWithChecking()
        {
            BlobClient blob = _container.GetBlobClient("imageBlob.jpg");

            Console.WriteLine("Checking blob lease state...");

            while (true)
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
                    await Task.Delay(1000);
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
