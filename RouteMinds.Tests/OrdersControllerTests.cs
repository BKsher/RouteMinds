using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using RouteMinds.API.Controllers;
using RouteMinds.API.DTOs;
using RouteMinds.Domain.Contracts;
using RouteMinds.Domain.Entities;
using RouteMinds.Domain.Interfaces;

namespace RouteMinds.Tests
{
    public class OrdersControllerTests
    {
        // We declare Mocks for the dependencies
        private readonly Mock<IOrderRepository> _mockRepo;
        private readonly Mock<IPublishEndpoint> _mockPublish;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            // 1. Setup Mocks
            _mockRepo = new Mock<IOrderRepository>();
            _mockPublish = new Mock<IPublishEndpoint>();
            _mockCache = new Mock<IDistributedCache>();

            // 2. Inject Mocks into Controller
            _controller = new OrdersController(
                _mockRepo.Object,
                _mockPublish.Object,
                _mockCache.Object
            );
        }

        [Fact] // [Fact] means "This is a test that is always true"
        public async Task CreateOrder_ShouldReturn201_WhenValidDto()
        {
            // Arrange (Prepare data)
            var dto = new CreateOrderDto
            {
                CustomerName = "Test User",
                Latitude = 10,
                Longitude = 10
            };

            // Act (Run the method)
            var result = await _controller.CreateOrder(dto);

            // Assert (Check results)
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);

            // Verify that the Repository.AddAsync was actually called once
            _mockRepo.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Once);

            // Verify that we published a message to RabbitMQ
            _mockPublish.Verify(p => p.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}