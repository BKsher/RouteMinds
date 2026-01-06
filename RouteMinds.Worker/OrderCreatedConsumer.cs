using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using RouteMinds.Domain.Contracts;
using RouteMinds.Domain.Interfaces;
using System.Text.Json; // Used for serializing data to Redis

namespace RouteMinds.Worker
{
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly ILogger<OrderCreatedConsumer> _logger;
        private readonly IOrderRepository _repository;
        private readonly IDistributedCache _cache; // Redis

        // Defined Hub Coordinates (Lat, Lon)
        private readonly (string Name, double Lat, double Lon)[] _hubs =
        {
            ("Berlin Hub", 52.5200, 13.4050),
            ("Hamburg Hub", 53.5511, 9.9937),
            ("Munich Hub", 48.1351, 11.5820)
        };

        public OrderCreatedConsumer(
            ILogger<OrderCreatedConsumer> logger,
            IOrderRepository repository,
            IDistributedCache cache)
        {
            _logger = logger;
            _repository = repository;
            _cache = cache;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var orderId = context.Message.OrderId;
            _logger.LogInformation("🚀 Processing Order #{OrderId}...", orderId);

            // 1. Get Order from DB
            var order = await _repository.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError("Order #{OrderId} not found in DB!", orderId);
                return;
            }

            // --- SIMULATE POISON MESSAGE ---
            if (order.CustomerName == "Joker")
            {
                _logger.LogError("🤡 Joker found! Simulating crash for Order #{Id}", orderId);
                throw new InvalidOperationException("Why so serious? (Crash caused by bad data)");
            }
            // -------------------------------

            // Idempotency Check
            if (!string.IsNullOrEmpty(order.RoutePlanJson))
            {
                _logger.LogWarning("⚠️ Order #{Id} was already processed. Skipping.", orderId);
                return; // Stop processing. We are done.
            }

            // 2. Logic: Find Nearest Hub
            var nearestHub = _hubs
                .OrderBy(h => CalculateDistance(order.Latitude, order.Longitude, h.Lat, h.Lon))
                .First();

            // 3. Logic: Generate "Route Plan"
            var routePlan = new
            {
                OrderId = orderId,
                Origin = nearestHub.Name,
                Destination = order.DeliveryAddress,
                EstimatedDistanceKm = Math.Round(CalculateDistance(order.Latitude, order.Longitude, nearestHub.Lat, nearestHub.Lon) * 111, 2), // 1 deg approx 111km
                ProcessedAt = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Route Calculated: Assigned to {Hub}. Distance: {Dist}km", routePlan.Origin, routePlan.EstimatedDistanceKm);

            // Save result to database --------------

            // Serialize to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(routePlan);

            // Write to the Entity
            order.RoutePlanJson = json;

            // Save changes to Database (The Billboard)
            await _repository.SaveChangesAsync();

            // ----------------

            // 4. Save to Redis (Cache)
            // Key: "route_10" -> Value: JSON String
            var cacheKey = $"route_{orderId}";
            var cacheValue = JsonSerializer.Serialize(routePlan);

            await _cache.SetStringAsync(cacheKey, cacheValue, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // Keep for 1 hour
            });

            _logger.LogInformation("💾 Result cached in Redis for Order #{OrderId}", orderId);
        }

        // Simple Euclidean distance (Good enough for resume demo)
        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var d1 = lat1 - lat2;
            var d2 = lon1 - lon2;
            return Math.Sqrt(d1 * d1 + d2 * d2);
        }
    }
}