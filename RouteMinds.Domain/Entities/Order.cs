namespace RouteMinds.Domain.Entities
{
    public class Order
    {
        // EF Core needs a Primary Key. By convention, 'Id' is the key.
        public int Id { get; set; }

        // This will become a VARCHAR/TEXT column
        public string CustomerName { get; set; } = string.Empty;

        public string DeliveryAddress { get; set; } = string.Empty;

        // We will need coordinates for your algorithm later
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public decimal PackageWeightKg { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? RoutePlanJson { get; set; }
    }
}