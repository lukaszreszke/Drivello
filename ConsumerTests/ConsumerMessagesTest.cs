using ContractTests;
using Drivello.EventHandlers;
using Drivello.Infrastructure;
using Drivello.Models;
using Microsoft.EntityFrameworkCore;
using NServiceBus.Testing;
using PactNet;
using Shared.Messages;
using Xunit.Abstractions;

namespace ConsumerTests;

public class ConsumerMessagesTest
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IMessagePactBuilderV4 _pactBuilder;

    public ConsumerMessagesTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;

        var config = new PactConfig
        {
            PactDir = Path.Join("..", "..", "..", "pacts"),
            Outputters = new[] { new XUnitOutput(_outputHelper) },
            LogLevel = PactLogLevel.Information
        };

        var pact = Pact.V4("LoyaltyService", "LoyaltyEvents", config);
        _pactBuilder = pact.WithMessageInteractions();
    }

    [Fact]
    public async Task GetLoyaltyPoints_PublishesEvent_WhenUserExists()
    {
    }
}