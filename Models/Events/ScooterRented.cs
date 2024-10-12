namespace Drivello.Models.Events;

public class ScooterRented(int userId, int scooterId)
{
    private readonly int _userId = userId;
    private readonly int _scooterId = scooterId;
}