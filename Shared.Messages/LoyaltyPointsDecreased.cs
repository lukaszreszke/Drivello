namespace Shared.Messages;

public class LoyaltyPointsDecreased : IEvent
{
    public int UserId { get; set; }
    public int Points { get; set; }
}