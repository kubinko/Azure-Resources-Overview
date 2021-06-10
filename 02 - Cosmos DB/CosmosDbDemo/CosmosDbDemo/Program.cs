using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using RandomNameGeneratorLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDbDemo
{
    class Program
    {
        private const string ConnectionString = "<connection string>";
        private const string ContainerName = "people";
        private const string PartitionKeyPath = "/city";

        private static readonly string[] _cities = { "Bratislava", "Košice", "Žilina" };
        private static readonly Random _random = new Random(Environment.TickCount);
        private static readonly PersonNameGenerator _nameGenerator = new PersonNameGenerator();

        private static Database _db;
        private static Container _container;

        static async Task Main(string[] args)
        {
            var options = new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Gateway,
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            using (var client = new CosmosClient(ConnectionString, options))
            {
                await CheckAccountInfo(client);

                //await InitializeContainer(client);

                //await UpdateDatabaseThroughput(500);
                //await CreateNewItem();
                //await UpdateItem("", "");
                //await DeleteItem("", "");
                //await RunStoredProcedure("Poprad");
                //await RunQuery("SELECT * FROM p WHERE p.city = \"Poprad\"");
                //await CreateItemWithTrigger();
            }
        }

        static async Task CheckAccountInfo(CosmosClient client)
        {
            AccountProperties accountInfo = await client.ReadAccountAsync();
            Console.WriteLine($"Id:                {accountInfo.Id}");
            Console.WriteLine($"Consistency level: {accountInfo.Consistency.DefaultConsistencyLevel}");
            Console.WriteLine($"Read reagions:     {string.Join(", ", accountInfo.ReadableRegions.Select(r => r.Name))}");
            Console.WriteLine($"Write regions:     {string.Join(", ", accountInfo.WritableRegions.Select(r => r.Name))}");
        }

        static async Task InitializeContainer(CosmosClient client)
        {
            Console.WriteLine("Initializing container...");

            _db = await client.CreateDatabaseIfNotExistsAsync("testdb", ThroughputProperties.CreateManualThroughput(400));
            _container = await _db.CreateContainerIfNotExistsAsync(ContainerName, PartitionKeyPath);

            Console.WriteLine($"Container {_container.Id} in database {_container.Database.Id} initialized successfully.");
        }

        static async Task UpdateDatabaseThroughput(int newThroughput)
        {
            int? current = await _db.ReadThroughputAsync();
            if (!current.HasValue)
            {
                Console.WriteLine($"Provisioned throughput for database is not specified.");
            }
            else
            {
                Console.WriteLine($"Current throughput: {current} RU/s");
            }

            Console.WriteLine("Updating database throughput...");

            ThroughputResponse response = await _db.ReplaceThroughputAsync(newThroughput);
            if (!response.Resource.Throughput.HasValue)
            {
                Console.WriteLine($"Provisioned throughput for database could not be set.");
            }
            else
            {
                Console.WriteLine($"New throughput:     {response.Resource.Throughput} RU/s");
            }
        }

        static async Task CreateNewItem()
        {
            string name = _nameGenerator.GenerateRandomFirstName();
            string surname = _nameGenerator.GenerateRandomLastName();

            var person = new Person()
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Surname = surname,
                City = _cities[_random.Next(_cities.Length)],
                Email = $"{surname.ToLower()}.{name.ToLower()}@kros.sk"
            };

            Console.WriteLine("Adding new record...");

            ItemResponse<Person> response = await _container.CreateItemAsync(person, new PartitionKey(person.City));

            Console.WriteLine($"Record added with id {response.Resource.Id}.");
            Console.WriteLine($"Request charge was {response.RequestCharge} RUs.");
        }

        static async Task UpdateItem(string partitionKey, string id)
        {
            ItemResponse<Person> response = await _container.ReadItemAsync<Person>(id, new PartitionKey(partitionKey));
            Person person = response.Resource;

            person.Name = "John";
            person.Surname = "Kramer";
            person.Email = "kramer.john@kros.sk";

            Console.WriteLine("Updating record...");

            response = await _container.ReplaceItemAsync(person, person.Id, new PartitionKey(partitionKey));

            Console.WriteLine($"Record updated.");
            Console.WriteLine($"Request charge was {response.RequestCharge} RUs.");
        }

        static async Task DeleteItem(string partitionKey, string id)
        {
            Console.WriteLine("Deleting record...");

            ItemResponse<Person> response = await _container.DeleteItemAsync<Person>(id, new PartitionKey(partitionKey));

            Console.WriteLine($"Record deleted.");
            Console.WriteLine($"Request charge was {response.RequestCharge} RUs.");
        }

        static async Task RunStoredProcedure(string city)
        {
            Console.WriteLine("Executing stored procedure...");

            Scripts scripts = _container.Scripts;
            StoredProcedureExecuteResponse<string> response = await scripts.ExecuteStoredProcedureAsync<string>(
                "spSeedPeople", new PartitionKey(city), new dynamic[] { 10, city });

            Console.WriteLine($"Result: {response.Resource}");

        }

        static async Task RunQuery(string query)
        {
            Console.WriteLine(query);
            Console.WriteLine();

            var queryDef = new QueryDefinition(query);
            double charge = 0;
            FeedIterator<Person> resultIterator = _container.GetItemQueryIterator<Person>(queryDef);
            while (resultIterator.HasMoreResults)
            {
                FeedResponse<Person> currentResultSet = await resultIterator.ReadNextAsync();
                foreach (var person in currentResultSet.Resource)
                {
                    Console.WriteLine($"{person.Surname} {person.Name}, {person.Email}, {person.City}");
                }
                charge += currentResultSet.RequestCharge;
            }

            Console.WriteLine();
            Console.WriteLine($"Total charge: {charge} RUs");
        }

        static async Task CreateItemWithTrigger()
        {
            string name = _nameGenerator.GenerateRandomFirstName();
            string surname = _nameGenerator.GenerateRandomLastName();

            var person = new Person()
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Surname = surname,
                City = _cities[_random.Next(_cities.Length)],
                Email = $"{surname.ToLower()}.{name.ToLower()}@kros.sk"
            };

            Console.WriteLine("Adding new record with trigger...");

            var options = new ItemRequestOptions()
            {
                PreTriggers = new List<string>() { "addValidity" }
            };
            ItemResponse<Person> response = await _container.CreateItemAsync(person, new PartitionKey(person.City), options);

            Console.WriteLine($"Record added with id {response.Resource.Id}.");
            Console.WriteLine($"Request charge was {response.RequestCharge} RUs.");
        }

        private class Person
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Surname { get; set; }
            public string City { get; set; }
            public string Email { get; set; }
        }
    }
}
