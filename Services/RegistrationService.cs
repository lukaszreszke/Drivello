using Drivello.Infrastructure;
using Drivello.Models;
using Microsoft.EntityFrameworkCore;

namespace Drivello.Services;

public class RegistrationService
{
    private readonly RentalDbContext _context;

    public RegistrationService(RentalDbContext context)
    {
        _context = context;
    }
    
    public void RegisterUser(string email, string password)
    {
        var user = new User
        {
            Email = email,
            Password = password
        };

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    public void RegisterNewScooter(decimal basePricePerMinute)
    {
        var scooter = new Scooter
        {
            BasePricePerMinute = basePricePerMinute,
            Status = "Available"
        };
        
        _context.Scooters.Add(scooter);
        _context.SaveChanges();
    }
}