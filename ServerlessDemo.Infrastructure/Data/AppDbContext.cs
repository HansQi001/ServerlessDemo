using Microsoft.EntityFrameworkCore;
using ServerlessDemo.Domain.Entities;

namespace ServerlessDemo.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        { 
        
        }
    }
}
