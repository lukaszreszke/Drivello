namespace Shared.Messages;

public class ScooterRented(int userId, int id) : IEvent
{
    public int UserId { get; set; } = userId;
    public int Id { get; set; } = id;
}