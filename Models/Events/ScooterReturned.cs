namespace Drivello.Services;

public class ScooterReturned(int userId, int scooterId)
{
    private readonly int _userId = userId;
    private readonly int _scooterId = scooterId;
}