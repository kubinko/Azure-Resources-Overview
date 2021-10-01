namespace AzureFunctionsDemo.Domain
{
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public Address Address { get; set; }

        public override string ToString()
        {
            return
                $"Id:         {Id}\n" +
                $"First name: {FirstName}\n" +
                $"Last name:  {LastName}\n" +
                $"Age:        {Age}\n" +
                $"Address:    {Address?.ToString() ?? "unknown"}";
        }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string AreaCode { get; set; }

        public override string ToString()
        {
            return $"{Street}, {AreaCode} {City}";
        }
    }
}
