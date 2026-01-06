using MassTransit;
using Microsoft.EntityFrameworkCore;
using RouteMinds.Domain.Interfaces;
using RouteMinds.Infrastructure.Persistence;
using RouteMinds.Infrastructure.Repositories;
using RouteMinds.Worker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // Hide Microsoft noise
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341") // Send to Seq
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog();

// 1. CONFIG: Database (Same as API)
// Note: We need to read the connection string. In Worker, config is accessed via builder.Configuration
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connString));

// 2. CONFIG: Dependency Injection
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 3. CONFIG: Redis (The Cache)
// Try to get the Redis connection string (we haven't set this in Azure, so it will be null)
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");

if (!string.IsNullOrEmpty(redisConnection))
{
    // Use Real Redis (Production with Budget, or Local Docker)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "RouteMinds_";
    });
}
else
{
    // Fallback: Use Server RAM (Free Azure Mode)
    // This works exactly like Redis but data is lost if the server restarts.
    builder.Services.AddDistributedMemoryCache();
}

// 4. CONFIG: MassTransit (The Bus)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.SetKebabCaseEndpointNameFormatter();

    if (builder.Environment.IsDevelopment())
    {
        // LOCALHOST: Use RabbitMQ
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
            cfg.ReceiveEndpoint("order-created-queue", e =>
            {
                // 1. Retry 5 times, wait 1s between attempts
                e.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(1)));

                // 2. Parallel processing limits
                e.PrefetchCount = 10;
                e.UseConcurrencyLimit(5);

                e.ConfigureConsumer<OrderCreatedConsumer>(context);
            });
        });
    }
    else
    {
        // AZURE: Use Service Bus
        x.UsingAzureServiceBus((context, cfg) =>
        {
            // We will read this from Azure Environment Variables later
            var connectionString = builder.Configuration.GetConnectionString("ServiceBusConnection");
            cfg.Host(connectionString);
            cfg.ReceiveEndpoint("order-created-queue", e =>
            {
                // RELIABILITY CONFIGURATION

                // 1. Retry Policy: If it crashes, try 5 times.
                // Wait 1s, then 2s, 5s, 10s... (Exponential Backoff)
                e.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(1)));

                // 2. Rate Limiting: Don't take more than 5 messages at once
                // This prevents "Spamming" from killing your CPU
                e.PrefetchCount = 10;
                e.UseConcurrencyLimit(5);

                e.ConfigureConsumer<OrderCreatedConsumer>(context);
            });
        });
    }
});

var host = builder.Build();
host.Run();