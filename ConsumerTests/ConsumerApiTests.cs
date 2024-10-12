using System.Net;
using ContractTests;
using Drivello.Services;
using Microsoft.Extensions.Configuration;
using PactNet;
using PactNet.Matchers;
using Xunit.Abstractions;

namespace ConsumerTests;

public class ConsumerApiTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IPactBuilderV4 _pactBuilder;
    private readonly IConfiguration _configuration;
    private readonly MockHttpClientFactory _mockClientFactory;

    public ConsumerApiTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;

        var config = new PactConfig
        {
            PactDir = Path.Join("..", "..", "..", "pacts"),
            Outputters = new[] { new XUnitOutput(_outputHelper) },
            LogLevel = PactLogLevel.Information
        };

        var pact = Pact.V4("LoyaltyService", "LoyaltyApi", config);
        _pactBuilder = pact.WithHttpInteractions(9876);
        _mockClientFactory = new MockHttpClientFactory();

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["LoyaltyApiBaseUrl"] = _mockClientFactory.BaseUri
        });
        _configuration = configurationBuilder.Build();
    }

    [Fact]
    public async Task GetLoyaltyPoints_WhenUserExists_ReturnsPoints()
    {
        // Arrange
        const int userId = 123;
        const int expectedPoints = 100;

        _pactBuilder
            .UponReceiving("A request for loyalty points for an existing user")
            .Given("User exists", new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "loyaltyPoints", expectedPoints.ToString() }
            })
            .WithRequest(HttpMethod.Get, $"/api/loyalty/{userId}")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json")
            .WithJsonBody(new TypeMatcher(new PointsResponse(expectedPoints)));

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            using var httpClient = _mockClientFactory.CreateClient();
            var loyaltyService = new LoyaltyServiceClient(httpClient, _configuration);

            // Act
            var points = await loyaltyService.GetLoyaltyPoints(userId);

            // Assert
            Assert.Equal(expectedPoints, points.Points);
        });
    }

    [Fact]
    public async Task GetLoyaltyPoints_WhenUserDoesNotExist_ThrowsException()
    {
        // Arrange
        const int nonExistentUserId = 999;

        _pactBuilder
            .UponReceiving("A request for loyalty points for a non-existent user")
            .Given("User does not exist", new Dictionary<string, string>
            {
                { "userId", nonExistentUserId.ToString() }
            })
            .WithRequest(HttpMethod.Get, $"/api/loyalty/{nonExistentUserId}")
            .WillRespond()
            .WithStatus(HttpStatusCode.NotFound);

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            using var httpClient = _mockClientFactory.CreateClient();
            var loyaltyService = new LoyaltyServiceClient(httpClient, _configuration);

            // Act 
            var response = await loyaltyService.GetLoyaltyPoints(nonExistentUserId);

            // Assert
            Assert.Equal(0, response.Points);
        });
    }

    [Fact]
    public async Task EarnLoyaltyPoints_WhenUserExists_ReturnsOk()
    {
        // Arrange
        const int userId = 123;

        _pactBuilder
            .UponReceiving("A request to earn loyalty points for an existing user")
            .Given("User exists and has no points", new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "loyaltyPoints", "100" }
            })
            .WithRequest(HttpMethod.Post, $"/api/loyalty/earn_points").WithJsonBody(new TypeMatcher(new { userId, points = 100 }))
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new { Points = 100 })
            .WithHeader("Content-Type", "application/json");

        await _pactBuilder.VerifyAsync(async _ =>
        {
            using var httpClient = _mockClientFactory.CreateClient();
            var loyaltyService = new LoyaltyServiceClient(httpClient, _configuration);
            await loyaltyService.EarnLoyaltyPoints(userId, 100);
        });
    }

    [Fact]
    public async Task GetPointsForAllUsers_ReturnsPointsForAllUsers()
    {
        _pactBuilder.UponReceiving("A request for loyalty points for all users")
            .Given("Users exist")
            .WithRequest(HttpMethod.Get, "/api/loyalty/all")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json")
            .WithJsonBody(new TypeMatcher(new[] { new { UserId = 1, Points = 100 }, new { UserId = 2, Points = 200 } }));

        await _pactBuilder.VerifyAsync(async _ =>
        {
            using var httpClient = _mockClientFactory.CreateClient();
            var loyaltyService = new LoyaltyServiceClient(httpClient, _configuration);
            await loyaltyService.GetPointsForAllUsers();
        });
    }
}