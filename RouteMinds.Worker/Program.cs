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
            cfg.ConfigureEndpoints(context);
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
            cfg.ConfigureEndpoints(context);
        });
    }
});

var host = builder.Build();
host.Run();