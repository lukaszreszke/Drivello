namespace Drivello.Services;

public class MailingService : IMailingService
{
    public Task SendMail(string email, string message, decimal totalPrice)
    {
        return Task.CompletedTask;
    }
}

public interface IMailingService
{
    public Task SendMail(string email, string message, decimal totalPrice);
}