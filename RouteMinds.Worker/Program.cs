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
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "RouteMinds_";
});

// 4. CONFIG: MassTransit (The Bus)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("order-created-queue", e =>
        {
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();