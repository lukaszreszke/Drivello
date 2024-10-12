using Drivello.Models;
using Microsoft.EntityFrameworkCore;

namespace Drivello.Infrastructure;

public class RentalDbContext : DbContext
{
    public DbSet<Rental> Rentals { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Scooter> Scooters { get; set; }
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Database=rentaldb;Username=myuser;Password=mypassword");
    }
}