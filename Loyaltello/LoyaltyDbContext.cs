using Microsoft.EntityFrameworkCore;

namespace Loyaltello;

public class LoyaltyDbContext : DbContext
{
    public LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options) : base(options) { }

    public DbSet<LoyaltyUser> Users { get; set; }
}