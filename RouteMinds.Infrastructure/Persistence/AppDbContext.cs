using Microsoft.EntityFrameworkCore;
using RouteMinds.Domain.Entities;
using System.Collections.Generic;

namespace RouteMinds.Infrastructure.Persistence
{
    // Inheriting from DbContext makes this an EF Core class
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // This line tells EF: "I want a table in the DB called 'Orders' based on the 'Order' class"
        public DbSet<Order> Orders { get; set; }
    }
}