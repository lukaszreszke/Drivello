using System.Net;
using System.Net.Http.Json;
using Drivello.Infrastructure;
using Drivello.Models;
using Drivello.Services;
using IntegrationTests;
using Polly;
using Range = Drivello.Models.Range;

namespace Drivello.IntegrationTests;

public class DrivelloLoyaltyIntegrationTests : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly DrivelloApplicationFactory _drivelloFactory;

    public DrivelloLoyaltyIntegrationTests(TestFixture fixture)
    {
        _drivelloFactory = fixture.DrivelloFactory;
    }

    [Fact]
    public async Task GetUserLoyaltyPoints_ReturnsSuccessStatusCode()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5001");
        await httpClient.PostAsync("/api/loyalty/seed", new StringContent(""));
        var dbContext = _drivelloFactory.GetScopedService<RentalDbContext>();
        dbContext.Users.Add(new User() { Id = 1, LoyaltyPoints = 0, Name = "Łukasz", Membership = "Gold", Email = "lukasz@example.com", Password = "password" });
        await dbContext.SaveChangesAsync();
        var drivelloClient = _drivelloFactory.CreateClient();

        // Act
        var response = await drivelloClient.GetAsync("/User/1/loyalty-points");

        // Assert
        EnsureSuccessStatusCode(response);
        var json = await response.Content.ReadFromJsonAsync<PointsResponse>();
        Assert.Equal(100, json.Points);
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1));

        await retryPolicy.ExecuteAsync(async () =>
        {
            var dbContext = _drivelloFactory.GetScopedService<RentalDbContext>();
            var user = await dbContext.Users.FindAsync(1);
            Assert.Equal(100, user.LoyaltyPoints);
        });    
    }

    [Fact]
    public async Task GetUserLoyaltyPoints_ReturnsSuccessStatusCodetest()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5001");
        await httpClient.PostAsync("/api/loyalty/seed", new StringContent(""));
        var dbContext = _drivelloFactory.GetScopedService<RentalDbContext>();
        dbContext.Users.Add(new User() { Id = 1, LoyaltyPoints = 0, Name = "Łukasz", Membership = "Gold", Email = "lukasz@example.com", Password = "password" });
        await dbContext.SaveChangesAsync();
        var drivelloClient = _drivelloFactory.CreateClient();

        // Act
        var response = await drivelloClient.GetAsync("/User/1/loyalty-points");

        // Assert
        EnsureSuccessStatusCode(response);
        var json = await response.Content.ReadFromJsonAsync<PointsResponse>();
        Assert.Equal(100, json.Points);
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1));

        await retryPolicy.ExecuteAsync(async () =>
        {
            var dbContext = _drivelloFactory.GetScopedService<RentalDbContext>();
            var user = await dbContext.Users.FindAsync(1);
            Assert.Equal(100, user.LoyaltyPoints);
        });    
    }
    
    public HttpResponseMessage EnsureSuccessStatusCode(HttpResponseMessage response)
    {
        try
        {
            return response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            throw new Exception($"Response status code does not indicate success: {response.StatusCode}, {response.Content.ReadAsStringAsync().Result}", e);
        }
    }
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}