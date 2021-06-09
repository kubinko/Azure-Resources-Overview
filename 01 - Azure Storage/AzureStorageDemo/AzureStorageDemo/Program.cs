using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AzureStorageDemo
{
    class Program
    {
        private const string ServiceUrl = "<storage account URL>";
        private const string ConnectionString = "<storage account connection string>";
        private const string SasSignature = "<SAS token>";

        private static BlobServiceClient _client;
        private static BlobContainerClient _container;

        static async Task Main(string[] args)
        {
            _client = GetClientViaConnectionString();
            //_client = GetClientViaAAD();
            //_client = GetClientViaSasSignature();

            //await ValidateConnection();
            await InitializeContainer("testcontainer");
            //await UploadBlob("sample_file.json", "newBlob.json");
            //await UploadBlob("8k_image.jpg", "imageBlob.jpg");
            //await ListBlobs();
            //await ReadBlobProperties("newBlob.json");
            //await UpdateBlobProperties("newBlob.json");
            //await ChangeBlobTier("newBlob.json");
            //await DownloadBlob("newBlob.json", "downloaded.json");
            //await DeleteBlob("newBlob.json");
            //GenerateSasToken("imageBlob.jpg");
            //await GenerateSasTokenFromPolicy("imageBlob.jpg");
            //await LeaseBlob("imageBlob.jpg");
            //await LeaseBlobWithBreak("imageBlob.jpg");
            await LeaseBlobInfinitely("imageBlob.jpg");
        }

        async static Task CreateBlockBlobAsync(string accountName, string containerName, string blobName)
        {
            // Construct the blob container endpoint from the arguments.
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
                                                        accountName,
                                                        containerName);

            // Get a credential and create a client object for the blob container.
            BlobContainerClient containerClient = new BlobContainerClient(new Uri(containerEndpoint),
                                                                            new DefaultAzureCredential());

            try
            {
                // Create the container if it does not exist.
                await containerClient.CreateIfNotExistsAsync();

                // Upload text to a new block blob.
                string blobContents = "This is a block blob.";
                byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(blobContents);

                using (MemoryStream stream = new MemoryStream(byteArray))
                {
                    await containerClient.UploadBlobAsync(blobName, stream);
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        private static BlobServiceClient GetClientViaAAD()
            => new BlobServiceClient(new Uri(ServiceUrl), new DefaultAzureCredential());

        private static BlobServiceClient GetClientViaConnectionString()
            => new BlobServiceClient(ConnectionString);

        private static BlobServiceClient GetClientViaSasSignature()
            => new BlobServiceClient(new Uri(ServiceUrl), new AzureSasCredential(SasSignature));

        private static async Task ValidateConnection()
        {
            Console.WriteLine("Retrieving account information...");

            try
            {
                Response<AccountInfo> info = await _client.GetAccountInfoAsync();
                Console.WriteLine($"Account kind: {info.Value.AccountKind}");
                Console.WriteLine($"SKU:          {info.Value.SkuName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occured:\n{ex.Message}");
            }
        }

        private static Task InitializeContainer(string containerName)
        {
            Console.WriteLine($"Initializing blob container '{containerName}'...");

            _container = _client.GetBlobContainerClient(containerName);
            return _container.CreateIfNotExistsAsync(PublicAccessType.None);
        }

        private static Task UploadBlob(string filePath, string blobName)
        {
            BlobClient blob = _container.GetBlobClient(blobName);
            var options = new BlobUploadOptions()
            {
                AccessTier = AccessTier.Hot,
                HttpHeaders = new BlobHttpHeaders()
                {
                    ContentType = "image/jpg"
                },
                Metadata = new Dictionary<string, string>(new KeyValuePair<string, string>[] {
                    new KeyValuePair<string, string>("custom_property", "custom value")
                }),
                ProgressHandler = new ProgressMarker() { TotalBytes = new FileInfo(filePath).Length }
            };

            Console.WriteLine($"Uploading blob from {filePath} to '{_container.Name}\\{blobName}'...");
            return blob.UploadAsync(filePath, options);
        }

        class ProgressMarker : IProgress<long>
        {
            public long TotalBytes { get; set; }

            public void Report(long value)
                => Console.WriteLine($"File upload at {value * 100 / TotalBytes}%...");
        }

        private static async Task ListBlobs()
        {
            Console.WriteLine($"Listing blobs in container {_container.Name}:");
            await foreach (BlobItem blobItem in _container.GetBlobsAsync())
            {
                Console.WriteLine($"  {blobItem.Name} ({blobItem.Properties.ContentLength:###,###,###} B), " +
                    $"{blobItem.Properties.AccessTier}, {(blobItem.Deleted ? "deleted" : "active")}");
            }
        }

        private static async Task ReadBlobProperties(string blobName)
        {
            BlobClient blob = _container.GetBlobClient(blobName);
            Response<BlobProperties> response = await blob.GetPropertiesAsync();

            Console.WriteLine("Blob properties:");
            Console.WriteLine($"Acces tier:           {response.Value.AccessTier}");
            Console.WriteLine($"Blob type:            {response.Value.BlobType}");
            Console.WriteLine($"Content disposition:  {response.Value.ContentDisposition}");
            Console.WriteLine($"Size:                 {response.Value.ContentLength:###,###,##0} bytes");
            Console.WriteLine($"Content type:         {response.Value.ContentType}");
            Console.WriteLine($"Created on:           {response.Value.CreatedOn}");
            Console.WriteLine($"Last modified:        {response.Value.LastModified}");
            Console.WriteLine("Metadata:");
            foreach (var meta in response.Value.Metadata)
            {
                Console.WriteLine($"    {meta.Key}: {meta.Value}");
            }
        }

        private static async Task UpdateBlobProperties(string blobName)
        {
            Console.WriteLine("OLD");
            Console.WriteLine("---");
            await ReadBlobProperties(blobName);
            Console.WriteLine();

            BlobClient blob = _container.GetBlobClient(blobName);

            var properties = new BlobHttpHeaders()
            {
                ContentDisposition = "attachment; filename=\"newName.json\""
            };

            var metadata = new Dictionary<string, string>();
            metadata.Add("custom_property", "new value");
            metadata.Add("another_property", "secret value");

            Console.WriteLine("Updating HTTP headers...");
            await blob.SetHttpHeadersAsync(properties);

            Console.WriteLine("Updating metadata...");
            await blob.SetMetadataAsync(metadata);

            Console.WriteLine();
            Console.WriteLine("NEW");
            Console.WriteLine("---");
            await ReadBlobProperties(blobName);
        }

        private static async Task ChangeBlobTier(string blobName)
        {
            BlobClient blob = _container.GetBlobClient(blobName);
            Console.WriteLine($"Current access tier: {(await blob.GetPropertiesAsync()).Value.AccessTier}");

            await blob.SetAccessTierAsync(AccessTier.Cool);

            var properties = await blob.GetPropertiesAsync();
            Console.WriteLine($"Access tier changed on: {properties.Value.AccessTierChangedOn}");
            Console.WriteLine($"New access tier: {properties.Value.AccessTier}");
        }

        private static async Task DownloadBlob(string blobName, string filePath)
        {
            Console.WriteLine($"Downloading blob from '{_container.Name}\\{blobName}' to {filePath}...");

            BlobClient blob = _container.GetBlobClient(blobName);
            Response<BlobDownloadInfo> response = await blob.DownloadAsync();
            using FileStream downloadStream = File.OpenWrite(filePath);
            await response.Value.Content.CopyToAsync(downloadStream);
            downloadStream.Close();

            Console.WriteLine("Download finished.");
        }

        private static async Task DeleteBlob(string blobName)
        {
            Console.WriteLine($"Deleting blob '{_container.Name}\\{blobName}'...");

            BlobClient blob = _container.GetBlobClient(blobName);
            Response<bool> response = await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);


            if (response.Value)
            {
                Console.WriteLine("Blob succesfully deleted.");
            }
            else
            {
                Console.WriteLine("Blob not found.");
            }
        }

        private static void GenerateSasToken(string blobName)
        {
            Console.WriteLine("Building blob SAS...");

            var builder = new BlobSasBuilder()
            {
                BlobContainerName = _container.Name,
                BlobName = blobName,
                Resource = "b"
            };
            builder.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Tag);
            builder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);

            BlobClient blob = _container.GetBlobClient(blobName);
            Console.WriteLine($"SAS URI: {blob.GenerateSasUri(builder)}");
        }

        private static async Task GenerateSasTokenFromPolicy(string blobName)
        {
            Console.WriteLine("Creating stored access policy...");

            var policy = new BlobSignedIdentifier()
            {
                Id = "newpolicy",
                AccessPolicy = new BlobAccessPolicy()
                {
                    PolicyStartsOn = DateTimeOffset.UtcNow,
                    PolicyExpiresOn = DateTimeOffset.UtcNow.AddYears(1),
                    Permissions = "r"
                }
            };
            await _container.SetAccessPolicyAsync(permissions: new BlobSignedIdentifier[] { policy });

            Console.WriteLine("Building blob SAS...");

            var builder = new BlobSasBuilder()
            {
                BlobContainerName = _container.Name,
                BlobName = blobName,
                Resource = "b",
                Identifier = "newpolicy"
            };

            BlobClient blob = _container.GetBlobClient(blobName);
            Console.WriteLine($"SAS URI: {blob.GenerateSasUri(builder)}");
        }

        private static async Task LeaseBlob(string blobName)
        {
            BlobClient blob = _container.GetBlobClient(blobName);
            var leaseClient = new BlobLeaseClient(blob);

            try
            {
                BlobLease lease = await leaseClient.AcquireAsync(new TimeSpan(0, 1, 0));
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine($"{60 - i} second(s) remaining on lease...");
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occured:\n{ex.Message}");
            }
            finally
            {
                await leaseClient.ReleaseAsync();
                Console.WriteLine("Lease released.");
            }
        }

        private static async Task LeaseBlobWithBreak(string blobName)
        {
            BlobClient blob = _container.GetBlobClient(blobName);
            var leaseClient = new BlobLeaseClient(blob);

            try
            {
                BlobLease lease = await leaseClient.AcquireAsync(new TimeSpan(0, 1, 0));
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine($"{60 - i} second(s) remaining on lease...");
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occured:\n{ex.Message}");
            }
            finally
            {
                BlobLease lease = await leaseClient.BreakAsync(new TimeSpan(0, 0, 10));
                Console.WriteLine($"Lease broken. {lease.LeaseTime} second(s) remaining on the lease...");
            }

            for (int i = 1; i < 10; i++)
            {
                await Task.Delay(1000);
                Console.WriteLine($"{10 - i} second(s) remaining on the lease...");
            }
        }

        private static async Task LeaseBlobInfinitely(string blobName)
        {
            BlobClient blob = _container.GetBlobClient(blobName);
            var leaseClient = new BlobLeaseClient(blob);

            try
            {
                BlobLease lease = await leaseClient.AcquireAsync(new TimeSpan(0, 0, -1));
                Console.WriteLine($"Infinite lease acquired.");

                int x = 1, y = 0, z = x / y;
                await leaseClient.ReleaseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occured:\n{ex.Message}");
            }
        }
    }
}
