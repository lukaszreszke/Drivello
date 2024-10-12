namespace Drivello.Services;

public class MailingService
{
    public Task SendMail(string email, string message, decimal totalPrice)
    {
        // sends an email
        return Task.CompletedTask;
    }
}