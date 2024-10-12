namespace Shared.Messages;

public class LoyaltyPointsEarned : IEvent
{
    public int UserId { get; set; }
    public int Points { get; set; }
}