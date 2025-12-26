using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.OrderService.Data
{
    /// <summary>
    /// Design-time factory for creating OrderDbContext instances during migrations.
    /// </summary>
    public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
    {
        /// <summary>
        /// Creates a new instance of OrderDbContext for design-time operations.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A new OrderDbContext instance.</returns>
        public OrderDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
            _ = optionsBuilder.UseNpgsql("Host=localhost;Database=order_design;Username=postgres;Password=postgres");
            return new OrderDbContext(optionsBuilder.Options);
        }
    }
}
