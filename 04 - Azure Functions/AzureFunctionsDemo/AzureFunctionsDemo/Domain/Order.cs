namespace AzureFunctionsDemo.Domain
{
    public class Order
    {
        public string Id { get; set; }
        public string Department { get; set; }
        public string ItemCode { get; set; }
        public float Quantity { get; set; }
        public string Color { get; set; }
    }
}
