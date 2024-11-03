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
    }

    [Fact]
    public async Task GetLoyaltyPoints_WhenUserDoesNotExist_ThrowsException()
    {
    }

    [Fact]
    public async Task EarnLoyaltyPoints_WhenUserExists_ReturnsOk()
    {
    }

    [Fact]
    public async Task GetPointsForAllUsers_ReturnsPointsForAllUsers()
    {
    }
}