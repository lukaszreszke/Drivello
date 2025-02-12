using Drivello.Infrastructure;
using Drivello.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Messages;

namespace Drivello.Services;

public class ScooterService
{
    public ScooterService(
        RentalDbContext dbContext,
        IMessageSession messageBus,
        IActiveRentalsCounter activeRentalsCounter,
        IMailingService mailingService,
        IUserManager userManager)
    {
        _dbContext = dbContext;
        _messageBus = messageBus;
        _activeRentalsCounter = activeRentalsCounter;
        _mailingService = mailingService;
        _userManager = userManager;
    }
    
    private readonly RentalDbContext _dbContext;
    private readonly IMessageSession _messageBus;
    private readonly IActiveRentalsCounter _activeRentalsCounter;
    private readonly IMailingService _mailingService;
    private readonly IUserManager _userManager;

    public async Task<OfferDto> ShowOffer(int userId, int scooterId)
    {
        var scooter = await _dbContext.Scooters.FindAsync(scooterId);
        if (scooter == null || scooter.Status != "Available" || scooter.IsUnderMaintenance) return new OfferDto();

        var pricePerMinute = _userManager.IsPremiumUser(userId) ? scooter.BasePricePerMinute * 0.8m : scooter.BasePricePerMinute;
        
        return new OfferDto
        {
            PricePerMinute = pricePerMinute,
            Range = scooter.Range
        };
    }
    
    public async Task<bool> RentScooter(int userId, int scooterId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null || user.ViolationCount > 3 || user.IsSuspended) return false;
        if (await HasActiveRental(userId)) return false;

        var scooter = await _dbContext.Scooters.FindAsync(scooterId);
        if (scooter == null || scooter.Status != "Available" || scooter.IsUnderMaintenance) return false;

        var rental = new Rental
        {
            UserId = userId,
            ScooterId = scooterId,
            StartTime = DateTime.UtcNow
        };

        _dbContext.Rentals.Add(rental);
        scooter.Status = "Rented";
        user.LastRentalDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        await _messageBus.Publish(new ScooterRented(user.Id, scooterId));

        _activeRentalsCounter.Increment();

        return true;
    }

    public async Task<bool> FinishRental(int rentalId)
    {
        var rental = await _dbContext.Rentals.Include(x => x.Scooter).FirstOrDefaultAsync(x => x.Id == rentalId);
        if (rental == null || rental.EndTime != null) return false;

        var user = await _dbContext.Users.FindAsync(rental.UserId);
        if (user == null || user.ViolationCount > 3 || user.IsSuspended) return false;

        rental.EndTime = DateTime.UtcNow;
        user.LastRentalDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        await _messageBus.Publish(new RentalEnded { UserId = user.Id });

        _activeRentalsCounter.Decrement();
        var pricePerMinute = _userManager.IsPremiumUser(user.Id) ? rental.Scooter.BasePricePerMinute * 0.8m : rental.Scooter.BasePricePerMinute;
        var totalPrice = Convert.ToDecimal(rental.EndTime.Value.Subtract(rental.StartTime).TotalHours) * pricePerMinute;
        
        await _mailingService.SendMail(user.Email, "You have returned a scooter. Thank you for using our service!", totalPrice);

        return true;
    }
    
    public async Task<bool> SendScooterForMaintenance(int scooterId, string description)
    {
        var scooter = await _dbContext.Scooters.FindAsync(scooterId);
        if (scooter == null) return false;

        if (scooter.Status == "Rented")
        {
            return false;
        }

        scooter.Status = "Maintenance";
        scooter.IsUnderMaintenance = true;
        scooter.LastMaintenanceDate = DateTime.UtcNow;

        var maintenanceRecord = new MaintenanceRecord
        {
            ScooterId = scooterId,
            MaintenanceDate = DateTime.UtcNow,
            Description = description,
            IsCompleted = false
        };

        _dbContext.MaintenanceRecords.Add(maintenanceRecord);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CompleteMaintenance(int maintenanceRecordId)
    {
        var record = await _dbContext.MaintenanceRecords
            .Include(m => m.Scooter)
            .FirstOrDefaultAsync(m => m.Id == maintenanceRecordId);

        if (record == null || record.IsCompleted) return false;

        record.IsCompleted = true;
        record.Scooter.IsUnderMaintenance = false;
        record.Scooter.Status = "Available";

        await _dbContext.SaveChangesAsync();

        return true;
    }


    private async Task<bool> HasActiveRental(int userId)
    {
        return await _dbContext.Rentals.AnyAsync(r => r.UserId == userId && r.EndTime == null);
    }
}