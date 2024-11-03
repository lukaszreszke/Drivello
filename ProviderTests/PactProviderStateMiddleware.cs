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
                }
                else if (providerState.State == "User does not exist")
                {
                }
                else if (providerState.State == "User exists and has no points")
                {
                }
                else if (providerState.State == "Users exist")
                {
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