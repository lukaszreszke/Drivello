namespace Shared.Messages
{
    public class RentalEnded : IEvent
    {
        public int UserId { get; set; }
    }
}