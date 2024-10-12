namespace Loyaltello.EventHandlers;

public class ScooterReturned(int UserId, int ScooterId)
{
     public int UserId { get; } = UserId;
     public int ScooterId { get; } = ScooterId;
}

public class AddPoints : IHandleMessages<ScooterReturned>
{
    private readonly LoyaltyDbContext _dbContext;
    private readonly ILogger<AddPoints> _logger;

    public AddPoints(LoyaltyDbContext dbContext, ILogger<AddPoints> logger)
    {
        _logger = logger;
    }
    
    public Task Handle(ScooterReturned message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Adding points for user {UserId}", message.UserId);
        throw new Exception("it    is    not    implemented");
       _dbContext.Users.Find(message.UserId).LoyaltyPoints += 50;
         return _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}