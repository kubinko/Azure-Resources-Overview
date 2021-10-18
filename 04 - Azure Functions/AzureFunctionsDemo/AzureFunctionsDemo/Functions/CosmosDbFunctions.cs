using AzureFunctionsDemo.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureFunctionsDemo.Functions
{
    public static class CosmosDbFunctions
    {
        [FunctionName(nameof(OrderChanged))]
        public static void OrderChanged(
            [CosmosDBTrigger(
                databaseName: "Eshop",
                collectionName: "Orders",
                ConnectionStringSetting = "CosmosDbConnectionString",
                LeaseCollectionName = "leases",
                CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<Document> input,
            ILogger log)
        {
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

            if (input != null && input.Count > 0)
            {
                log.LogInformation("Documents modified " + input.Count);
                foreach (Document d in input)
                {
                    var order = JsonSerializer.Deserialize<Order>(d.ToString(), options);
                    log.LogInformation($"Modified order with ID {d.Id} - {order.ItemCode}, {order.Quantity:0.00}");
                }
            }
        }

        [FunctionName(nameof(FindOrder))]
        public static IActionResult FindOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "order/{partitionKey}/{id}")] HttpRequest req,
            [CosmosDB(
                databaseName: "Eshop",
                collectionName: "Orders",
                ConnectionStringSetting = "CosmosDbConnectionString",
                PartitionKey = "{partitionKey}",
                Id = "{id}")] Order order,
            ILogger log)
        {
            if (order != null)
            {
                return new OkObjectResult(order);
            }
            else
            {
                return new NotFoundResult();
            }
        }

        [FunctionName(nameof(FindOrders))]
        public static IActionResult FindOrders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{department}")] HttpRequest req,
            [CosmosDB(
                databaseName: "Eshop",
                collectionName: "Orders",
                ConnectionStringSetting = "CosmosDbConnectionString",
                SqlQuery = "SELECT * FROM o WHERE o.department = {department}")] IEnumerable<Order> orders,
            ILogger log)
        {
            if (orders != null)
            {
                return new OkObjectResult(orders);
            }
            else
            {
                return new NotFoundResult();
            }
        }

        [FunctionName(nameof(CreateOrder))]
        public static async Task<IActionResult> CreateOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequest req,
            [CosmosDB(
                databaseName: "Eshop",
                collectionName: "Orders",
                ConnectionStringSetting = "CosmosDbConnectionString")] DocumentClient client,
            ILogger log)
        {
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            var order = JsonSerializer.Deserialize<Order>(await req.ReadAsStringAsync(), options);
            log.LogInformation($"Received request for order {order.ItemCode}, {order.Quantity:0.00}");

            var requestOptions = new RequestOptions()
            {
                JsonSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
                    {
                        NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
                    }
                }
            };
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("Eshop", "Orders");
            var response = await client.CreateDocumentAsync(collectionUri, order, requestOptions);

            return new OkObjectResult(response.Resource.Id);
        }
    }
}
