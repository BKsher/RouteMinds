using MassTransit;
using Microsoft.EntityFrameworkCore;
using RouteMinds.Domain.Interfaces;
using RouteMinds.Infrastructure.Persistence;
using RouteMinds.Infrastructure.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341") // Send logs to Docker container
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddMassTransit(x =>
{
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

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 1. AUTO-MIGRATE DATABASE
// This forces the container to create tables in Azure on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // Applies any pending migrations
}

// 2. ENABLE SWAGGER ALWAYS (Remove the 'if IsDevelopment' check)
// For a public demo, we want Swagger visible in Production
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
