using Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

public class EcommerceDbContext : DbContext
{
    public EcommerceDbContext(DbContextOptions<EcommerceDbContext> options)
        : base(options) { }

    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Category> Categories { get; set; } = default!;
    public DbSet<Order> Orders { get; set; } = default!;
    public DbSet<OrderItem> OrderItems { get; set; } = default!;

}
