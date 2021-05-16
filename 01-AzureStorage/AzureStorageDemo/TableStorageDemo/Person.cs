using Microsoft.Azure.Cosmos.Table;

namespace TableStorageDemo
{
    class Person : TableEntity
    {
        public string Region { get => PartitionKey; set => PartitionKey = value; }
        public string IdNumber { get => RowKey; set => RowKey = value; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public int Age { get; set; }
        public string Nickname { get; set; }

        public Person()
        {
        }

        public Person(string region, string idNumber)
        {
            PartitionKey = region;
            RowKey = idNumber;
        }

        public override string ToString()
            => $"Region: {Region}, Id: {IdNumber}, Name: {Name}, Surname: {Surname}, Age: {Age}, Nickname: {Nickname}";
    }
}
