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
        // Arrange
        const int userId = 123;
        const int expectedPoints = 100;

        _pactBuilder
            .ExpectsToReceive("A event for loyalty points for an existing user")
            .Given("User exists", new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "loyaltyPoints", expectedPoints.ToString() }
            }).WithJsonContent(new
            {
                UserId = 123,
                Points = 100
            }).Verify<LoyaltyPointsEarned>(message =>
            {
                Assert.Equal(123, message.UserId);
                Assert.Equal(100, message.Points);
                
                var dbContext = new RentalDbContext(new DbContextOptionsBuilder<RentalDbContext>().UseInMemoryDatabase("RentalDb").Options);
                var messageHandleContext = new TestableMessageHandlerContext();

                dbContext.Users.Add(new User()
                {
                    Id = 123,
                    LoyaltyPoints = 0,
                    Password = "password",
                    Email = "lukasz@example.com",
                    Name = "Lukasz",
                    Membership = "Gold"
                });
                dbContext.SaveChanges();
                
                var loyaltyPointsEarned = new LoyaltyPointsEarnedHandler(dbContext);
                loyaltyPointsEarned.Handle(message, messageHandleContext).Wait();

                var user = dbContext.Users.Find(userId);
                Assert.Equal(expectedPoints, user.LoyaltyPoints);
            });
    }
}