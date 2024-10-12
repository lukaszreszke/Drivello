using Drivello.Infrastructure;
using Drivello.Models;
using NServiceBus;
using Shared.Messages;

namespace Drivello.EventHandlers
{
    public class LoyaltyPointsEarnedHandler : IHandleMessages<LoyaltyPointsEarned>
    {
        private readonly RentalDbContext _dbContext;

        public LoyaltyPointsEarnedHandler(RentalDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(LoyaltyPointsEarned message, IMessageHandlerContext context)
        {
            var user = await _dbContext.Users.FindAsync(message.UserId);
            if (user == null)
            {
                user = new User
                {
                    Id = message.UserId,
                    LoyaltyPoints = message.Points
                };
                _dbContext.Users.Add(user);
            }
            else
            {
                user.LoyaltyPoints = message.Points;
            }

            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}