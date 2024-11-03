using System.ComponentModel.DataAnnotations;
using Drivello.Infrastructure;
using Drivello.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Messages;

namespace Drivello;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddDbContext<RentalDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });
        builder.UseNServiceBus(builder.AddNServiceBus(builder.Configuration));
        builder.Services.AddScoped<ScooterService>();
        builder.Services.AddScoped<IActiveRentalsCounter, ActiveRentalsCounter>();
        builder.Services.AddScoped<IMailingService, MailingService>();
        builder.Services.AddScoped<IUserManager, UserManager>();
        
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHealthChecks("/health");
        app.UseHttpsRedirection();

        app.MapGet("/user/{id}/loyalty-points", async (int id, [FromServices] ILoyaltyService loyaltyService, [FromServices] IMessageSession messageSession) =>
        {
            var points = await loyaltyService.GetLoyaltyPoints(id);
            return Results.Ok(points);
        });

        app.MapPost("/scooter/{id}/rent", async ([Required] int id, [FromBody] RentalRequest rental, [FromServices] ScooterService scooterService) =>
        {
            await scooterService.RentScooter(rental.UserId, id);
        });

        app.MapPost("/scooter/{id}/finish", async ([Required] int id, [FromServices] ScooterService scooterService) =>
        {
            await scooterService.FinishRental(id);
        });
        
        app.MapPost("/users/register", async ([FromBody] RegistrationRequest request, [FromServices] RegistrationService registrationService) =>
        {
            registrationService.RegisterUser(request.Email, request.Password, request.Name);
            return Task.CompletedTask;
        });
        
        app.MapPost("/scooters/new", async ([FromBody] decimal basePricePerMinute, [FromServices] RegistrationService registrationService) =>
        {
            registrationService.RegisterNewScooter(basePricePerMinute);
            return Task.CompletedTask;
        });
        
        app.Run();
    }
}

public record RegistrationRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
}

public record RentalRequest(int UserId);