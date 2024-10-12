using Drivello.Infrastructure;

namespace Drivello.Services;

public class UserManager : IUserManager
{
    private readonly RentalDbContext _context;

    public UserManager(RentalDbContext context)
    {
        _context = context;
    }
    
    public bool IsPremiumUser(int userId)
    {
        var user = _context.Users.Find(userId);
        return user?.Membership == "Premium";
    }
}

public interface IUserManager
{
    bool IsPremiumUser(int userId);
}