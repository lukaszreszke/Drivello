using Drivello.Models;
using Microsoft.EntityFrameworkCore;

namespace Drivello.Infrastructure;

public class RentalDbContext(DbContextOptions<RentalDbContext> options) : DbContext(options)
{
    public RentalDbContext() : this(new DbContextOptions<RentalDbContext>())
    {
    }
    
    public virtual DbSet<Rental> Rentals { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Scooter> Scooters { get; set; }
    public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
    public virtual DbSet<Issue> Issues { get; set; }
}