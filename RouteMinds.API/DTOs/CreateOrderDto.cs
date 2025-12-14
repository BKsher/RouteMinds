namespace RouteMinds.API.DTOs
{
    // This is what the user sends to create an order.
    // Notice: No "Id" (database generates it) and no "CreatedAt" (server sets it).
    public class CreateOrderDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal PackageWeightKg { get; set; }
    }
}