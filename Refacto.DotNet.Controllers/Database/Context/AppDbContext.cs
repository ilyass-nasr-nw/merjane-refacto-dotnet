using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Database.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public AppDbContext() : base()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure many-to-many relationship between Order and Product
            _ = modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithMany(); // Since Product doesn't have a collection for Order

            _ = modelBuilder.Entity<Product>()
                .Property(p => p.Type)
                .HasConversion(
                    type => type.ToString().ToUpperInvariant(),
                    value => Enum.Parse<ProductType>(value, true));
        }

        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Product> Products { get; set; }

    }
}
