using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Messages;

namespace Loyaltello.Controllers;

[ApiController]
public class LoyaltyController : ControllerBase
{
    private readonly LoyaltyDbContext _context;
    private IMessageSession _messageSession;

    public LoyaltyController(LoyaltyDbContext context, IMessageSession messageSession)
    {
        _context = context;
        _messageSession = messageSession;
    }

    [HttpGet("/api/loyalty/{userId}")]
    public async Task<ActionResult<int>> GetLoyaltyPoints(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        await _messageSession.Publish(new LoyaltyPointsEarned
        {
            UserId = user.Id,
            Points = 100,
        });

        return Ok(new { Points = user.LoyaltyPoints });
    }

    [HttpPost("/api/loyalty/earn_points")]
    public async Task<IActionResult> EarnPoints(EarnPointsRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            return NotFound($"User {request.UserId} not found");
        }

        user.LoyaltyPoints += request.Points;
        await _context.SaveChangesAsync();

        await _messageSession.Publish(new LoyaltyPointsEarned
        {
            UserId = request.UserId,
            Points = request.Points,
        });

        return Ok(new PointsResponse(user.LoyaltyPoints));
    }
    
    [HttpGet("/api/loyalty/all")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users.Select(u => new { UserId = u.Id, Points = u.LoyaltyPoints }));
    }

    [HttpPost("/api/loyalty/seed")]
    public async Task<ActionResult> Seed()
    {
        var user = new LoyaltyUser { Id = 1, LoyaltyPoints = 100 };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok();
    }

    public record PointsResponse (int Points);
    
    public class EarnPointsRequest
    {
        public int UserId { get; set; }
        public int Points { get; set; }
    }
}