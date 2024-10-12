using Shared.Messages;

namespace Loyaltello.EventHandlers
{
    public class RentalEndedHandler : IHandleMessages<RentalEnded>
    {
        private readonly LoyaltyDbContext _dbContext;

        public RentalEndedHandler(
            LoyaltyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(RentalEnded message, IMessageHandlerContext context)
        {
            var user = await _dbContext.Users.FindAsync(message.UserId, context.CancellationToken);
            user.LoyaltyPoints += 50;
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}