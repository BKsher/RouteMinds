using Microsoft.AspNetCore.Mvc;
using RouteMinds.API.DTOs;
using RouteMinds.Domain.Entities;
using RouteMinds.Domain.Interfaces;
using MassTransit;
using RouteMinds.Domain.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace RouteMinds.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // This maps to http://localhost:xxxx/api/orders
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _repository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IDistributedCache _cache;

        // Constructor Injection: The API asks for the Interface, 
        // and Program.cs provides the Repository we registered earlier.
        public OrdersController(IOrderRepository repository, IPublishEndpoint publishEndpoint, IDistributedCache cache)
        {
            _repository = repository;
            _publishEndpoint = publishEndpoint;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            // 1. Map DTO to Domain Entity
            var order = new Order
            {
                CustomerName = dto.CustomerName,
                DeliveryAddress = dto.DeliveryAddress,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                PackageWeightKg = dto.PackageWeightKg,
                CreatedAt = DateTime.UtcNow
            };

            // 2. Use Repository to save
            await _repository.AddAsync(order);
            await _repository.SaveChangesAsync();

            // 3. Publish Event (Asynchronous part)
            // We put the letter in the mailbox. We don't wait for it to be delivered.
            await _publishEndpoint.Publish(new OrderCreatedEvent(order.Id));

            // 4. Return 201 Created
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _repository.GetByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _repository.GetAllAsync();
            return Ok(orders);
        }

        [HttpGet("{id}/route")]
        public async Task<IActionResult> GetRoute(int id)
        {
            // 1. Construct the Key (Must match what the Worker saved)
            var cacheKey = $"route_{id}";

            // 2. Try to fetch from Redis
            var cachedData = await _cache.GetStringAsync(cacheKey);

            // 3. Handle Cache Miss
            if (string.IsNullOrEmpty(cachedData))
            {
                // Optional: Check if order exists in DB first to distinguish "Not Found" vs "Pending"
                return Accepted("Route is being calculated. Please try again in a few seconds.");
            }

            // 4. Return the Data
            // We deserialize to 'object' so ASP.NET returns it as proper JSON, not a string with escaped quotes
            var routePlan = JsonSerializer.Deserialize<object>(cachedData);

            return Ok(routePlan);
        }
    }
}