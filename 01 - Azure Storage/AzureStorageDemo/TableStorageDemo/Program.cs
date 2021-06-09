using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableStorageDemo
{
    class Program
    {
        private const string ConnectionString = "<connection string>";

        private static CloudStorageAccount _account;
        private static CloudTableClient _tableClient;
        private static CloudTable _table;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Connecting to account...");
            _account = CloudStorageAccount.Parse(ConnectionString);

            Console.WriteLine("Initializing table client...");
            _tableClient = _account.CreateCloudTableClient();

            await InitializeTable("people");
            ListAllTables();

            //await InsertPerson();
            //await InsertPeople();
            //await UpdatePeople();
            //QueryPeople();
            //await DeletePerson();
            //ListAllPeople();
        }

        private static void ListAllTables()
        {
            Console.WriteLine("Tables in storage:");

            IEnumerable<CloudTable> tables = _tableClient.ListTables();
            if (tables.Any())
            {
                foreach (var table in tables)
                {
                    Console.WriteLine($"  {table.Name}");
                }
            }
            else
            {
                Console.WriteLine("  No tables found.");
            }
        }

        private static async Task InitializeTable(string tableName)
        {
            Console.WriteLine($"Initializing table '{tableName}'...");
            _table = _tableClient.GetTableReference(tableName);

            if (await _table.CreateIfNotExistsAsync())
            {
                Console.WriteLine("Table created.");
            }
        }

        private static void ListAllPeople()
        {
            var query = new TableQuery<Person>();
            IEnumerable<Person> results = _table.ExecuteQuery(query);

            Console.WriteLine("People in table:");
            if (results.Any())
            {
                foreach (var person in results)
                {
                    Console.WriteLine($"  {person}");
                }
            }
            else
            {
                Console.WriteLine("  No people found.");
            }
        }

        private static Task InsertPerson()
        {
            var person = new Person("DC", "1")
            {
                Name = "Clark",
                Surname = "Kent",
                Age = 33
            };
            TableOperation operation = TableOperation.Insert(person);
            return _table.ExecuteAsync(operation);
        }

        private static Task InsertPeople()
        {
            var batchOperation = new TableBatchOperation();
            batchOperation.Insert(new Person("Marvel", "1") { Name = "Tony", Surname = "Stark", Age = 50/*, Nickname = "Iron man"*/ });
            batchOperation.Insert(new Person("Marvel", "2") { Name = "Steve", Surname = "Rogers", Age = 30/*, Nickname = "Captain America"*/ });
            batchOperation.Insert(new Person("Marvel", "3") { Name = "Bruce", Surname = "Banner", Age = 42/*, Nickname = "Hulk"*/ });

            return _table.ExecuteBatchAsync(batchOperation);
        }

        private static async Task UpdatePeople()
        {
            var person = new Person("DC", "2")
            {
                Name = "Bruce",
                Surname = "Wayne",
                Age = 45/*,
                Nickname = "Batman"*/
            };
            TableOperation operation = TableOperation.InsertOrMerge(person);
            await _table.ExecuteAsync(operation);

            var columns = new List<string>(new string[] { nameof(person.PartitionKey), nameof(person.RowKey) });
            TableOperation retrieveOperation = TableOperation.Retrieve<Person>("DC", "1", columns);
            var result = await _table.ExecuteAsync(retrieveOperation);
            var retrievedPerson = result.Result as Person;
            //retrievedPerson.Nickname = "Superman";
            operation = TableOperation.InsertOrMerge(retrievedPerson);
            await _table.ExecuteAsync(operation);
        }

        private static void QueryPeople()
        {
            var query = new TableQuery<Person>();
            query.FilterString = TableQuery.GenerateFilterCondition(nameof(Person.Name), QueryComparisons.Equal, "Bruce");
            //query.FilterString = TableQuery.GenerateFilterCondition(nameof(Person.PartitionKey), QueryComparisons.Equal, "DC");
            var results = _table.ExecuteQuery(query);

            Console.WriteLine($"Query condition: {query.FilterString}");
            Console.WriteLine("Query results:");
            foreach (var person in results)
            {
                Console.WriteLine($"  {person}");
            }
        }

        private static async Task DeletePerson()
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<Person>("Marvel", "1");
            var result = await _table.ExecuteAsync(retrieveOperation);
            var person = result.Result as Person;

            TableOperation operation = TableOperation.Delete(person);
            await _table.ExecuteAsync(operation);
        }
    }
}
