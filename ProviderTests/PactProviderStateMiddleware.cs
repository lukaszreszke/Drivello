using System.Text.Json;
using System.Text.Json.Serialization;
using Loyaltello;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoyaltyApi.Tests.Pacts;

public class PactProviderStateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public PactProviderStateMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == "POST" && context.Request.Path.Value == "/provider-states")
        {
            using var reader = new StreamReader(context.Request.Body);
            var jsonString = await reader.ReadToEndAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var providerState = JsonSerializer.Deserialize<ProviderState>(jsonString, options);
            if (providerState != null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<LoyaltyDbContext>();

                if (providerState.State == "User exists")
                {
                    var userId = int.Parse(providerState.Params["userId"].ToString()!);
                    var points = int.Parse(providerState.Params["loyaltyPoints"].ToString()!);

                    var user = await dbContext.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.LoyaltyPoints = points;
                    }
                    else
                    {
                        dbContext.Users.Add(new LoyaltyUser() 
                        {
                            Id = userId,
                            LoyaltyPoints = points
                        });
                    }
                    await dbContext.SaveChangesAsync();
                }
                else if (providerState.State == "User does not exist")
                {
                    var userId = int.Parse(providerState.Params["userId"].ToString()!);
                    var user = await dbContext.Users.FindAsync(userId);
                    if (user != null)
                    {
                        dbContext.Users.Remove(user);
                        await dbContext.SaveChangesAsync();
                    }
                }
                else if (providerState.State == "User exists and has no points")
                {
                    var userId = int.Parse(providerState.Params["userId"].ToString()!);
                    var user = await dbContext.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.LoyaltyPoints = 0;
                    }
                    else
                    {
                        dbContext.Users.Add(new LoyaltyUser() 
                        {
                            Id = userId,
                            LoyaltyPoints = 0
                        });
                    }
                    await dbContext.SaveChangesAsync();
                }
                else if (providerState.State == "Users exist")
                {
                    var users = await dbContext.Users.ToListAsync();
                    dbContext.Users.RemoveRange(users);
                    await dbContext.SaveChangesAsync();
                    
                    var usersData = new List<LoyaltyUser>
                    {
                        new LoyaltyUser { Id = 1, LoyaltyPoints = 100 },
                        new LoyaltyUser { Id = 2, LoyaltyPoints = 200 },
                    };
                    
                    await dbContext.Users.AddRangeAsync(usersData);
                    await dbContext.SaveChangesAsync();
                }
            }

            await context.Response.WriteAsJsonAsync(String.Empty);
        }
        else
        {
            await _next(context);
        }
    }
}