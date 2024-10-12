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
    private readonly Func<Task> _resetDatabaseAsync;

    public DrivelloLoyaltyIntegrationTests(TestFixture fixture)
    {
        _drivelloFactory = fixture.DrivelloFactory;
        _resetDatabaseAsync = fixture.ResetDatabaseAsync;
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
    public async Task rental()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5001");
        await httpClient.PostAsync("/api/loyalty/seed", new StringContent(""));
        
        var dbContext = _drivelloFactory.GetScopedService<RentalDbContext>();
        dbContext.Users.Add(new User() { Id = 1, LoyaltyPoints = 0, Name = "Łukasz", Membership = "Gold", Email = "lukasz@example.com", Password = "password" });
        dbContext.Scooters.Add(new Scooter() { Id = 1, BasePricePerMinute = 1, Range = Range.Long, Status = "Available", IsUnderMaintenance = false });
        await dbContext.SaveChangesAsync();
        var drivelloClient = _drivelloFactory.CreateClient();
        var response = await drivelloClient.PostAsJsonAsync("/scooter/1/rent", new RentalRequest(1));
        response.EnsureSuccessStatusCode();

        var finishResponse = await drivelloClient.PostAsJsonAsync("/scooter/1/finish", new {});
        EnsureSuccessStatusCode(finishResponse);
        
        var retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1));
        await retryPolicy.ExecuteAsync(async () =>
        {
            var allUsersWithPoints = await httpClient.GetAsync("api/loyalty/all");
            allUsersWithPoints.EnsureSuccessStatusCode();
            var json = await allUsersWithPoints.Content.ReadFromJsonAsync<List<PointsResponse>>();
            Assert.Equal(150, json.First().Points);
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

    public async Task DisposeAsync()
    {
        await _resetDatabaseAsync();
    }
}