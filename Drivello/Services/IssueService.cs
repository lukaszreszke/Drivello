using Drivello.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Drivello.Services;

using Models;
using System;
using System.Threading.Tasks;

public class IssueService : IIssueService
{
    private readonly RentalDbContext _context;
    private readonly IUserNotificationService _userNotificationService;
    private readonly IUserManager _userManager;

    private const decimal AutomaticRefundThreshold = 20.0m;
    private const decimal AutomaticRefundForPremiumThreshold = 50.0m;

    public IssueService(
        RentalDbContext context,
        IUserNotificationService userNotificationService,
        IUserManager userManager)
    {
        _context = context;
        _userNotificationService = userNotificationService;
        _userManager = userManager;
    }

    public async Task<Issue> Create(IssueDto issueDto)
    {
        var issue = new Issue
        {
            CreatedAt = DateTime.UtcNow
        };
        issue = await Update(issueDto, issue);
        return issue;
    }

    public async Task<Issue> Find(int id)
    {
        var issue = await _context.Issues.FindAsync(id);
        if (issue == null)
        {
            throw new InvalidOperationException("Issue does not exist");
        }

        return issue;
    }

    public async Task<Issue> Update(IssueDto issueDto, Issue issue)
    {
        var user = await _context.Users.FindAsync(issueDto.UserId);
        var rental = await _context.Rentals.FindAsync(issueDto.RentalId);
        if (user == null)
        {
            throw new InvalidOperationException("User does not exist");
        }

        if (rental == null)
        {
            throw new InvalidOperationException("Rental does not exist");
        }

        issue.Status = issueDto.IsDraft ? Issue.Statuses.Draft : Issue.Statuses.New;
        issue.User = user;
        issue.Rental = rental;
        issue.CreatedAt = DateTime.UtcNow;
        issue.Reason = issueDto.Reason;
        issue.Description = issueDto.Description;
        await _context.SaveChangesAsync();
        return issue;
    }

    public async Task<Issue> ChangeStatus(Issue.Statuses newStatus, int id)
    {
        var issue = await Find(id);
        issue.Status = newStatus;
        return issue;
    }

    public async Task<Issue> CloseAutomaticallyIfPossible(int id)
    {
        var issue = await Find(id);
        var userIssues = await _context.Issues.Where(i => i.User.Id == issue.User.Id).ToListAsync();
        var rentalCost = RentalCost(issue.Rental);
        if (userIssues.Count <= 3 && rentalCost < AutomaticRefundThreshold)
        {
            issue.Status = Issue.Statuses.Refunded;
            issue.CompletionDate = DateTime.UtcNow;
            issue.CompletionMode = Issue.CompletionModes.Automatic;
            _userNotificationService.NotifyUserAboutRefund(issue.Id, issue.User.Id);
        }
        else if (issue.User.Membership == "Premium")
        {
            if (rentalCost < AutomaticRefundForPremiumThreshold)
            {
                issue.Status = Issue.Statuses.Refunded;
                issue.CompletionDate = DateTime.UtcNow;
                issue.CompletionMode = Issue.CompletionModes.Automatic;
                _userNotificationService.NotifyUserAboutRefund(issue.Id, issue.User.Id);
            }
            else
            {
                issue.Status = Issue.Statuses.Escalated;
                issue.CompletionMode = Issue.CompletionModes.Manual;
                _userNotificationService.GetMoreDetails(issue.Id, issue.User.Id);
            }
        }
        else
        {
            issue.Status = Issue.Statuses.Escalated;
            issue.CompletionMode = Issue.CompletionModes.Manual;
            _userNotificationService.GetMoreDetails(issue.Id, issue.User.Id);
        }

        issue.ChangedAt = DateTime.UtcNow;
        return issue;
    }

    private decimal RentalCost(Rental rental)
    {
        if (rental.EndTime.HasValue)
        {
            var pricePerMinute = _context.Scooters.Find(rental.ScooterId).BasePricePerMinute;
            if (_userManager.IsPremiumUser(rental.UserId))
            {
                pricePerMinute *= 0.8m;
            }
            else
            {
                pricePerMinute *= 1.0m;
            }

            return (decimal)(rental.EndTime.Value - rental.StartTime).TotalMinutes * pricePerMinute;
        }

        throw new InvalidOperationException("Rental has not ended yet");
    }
}

public interface IUserNotificationService
{
    void NotifyUserAboutRefund(int issueNo, int userId);
    void GetMoreDetails(int issueNo, int userId);
}

public interface IIssueService
{
    Task<Issue> Create(IssueDto issueDto);
    Task<Issue> Find(int id);
    Task<Issue> Update(IssueDto issueDto, Issue issue);
    Task<Issue> ChangeStatus(Issue.Statuses newStatus, int id);
    Task<Issue> CloseAutomaticallyIfPossible(int id);
}