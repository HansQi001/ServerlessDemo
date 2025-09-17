using Microsoft.EntityFrameworkCore;
using ServerlessDemo.FunApp.Models.Entities;

namespace ServerlessDemo.FunApp.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().ToTable("Products");

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

                entity.Property(p => p.CreatedAt)
                .HasColumnName("created_at");

                entity.Property(p => p.LastModifiedAt)
                .HasColumnName(@"updated_at");
            });

        }
    }
}
