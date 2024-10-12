using Drivello.Infrastructure;
using Drivello.Services;
using Microsoft.EntityFrameworkCore;
using NServiceBus.Testing;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace DrivelloTests;

public class ScooterServiceTests
{
    [Fact]
    public async Task eligible_user_can_rent_scooter()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterUser("email@lukaszreszke.pl", "password", "Łukasz");
        registrationService.RegisterNewScooter(0.5m);
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);
        var userId = context.Users.Last().Id;
        var scooterId = context.Scooters.Last().Id;

        // When
        var result = await rentalService.RentScooter(userId, scooterId);

        // Then
        Assert.True(result);
        activeRentalsCounter.Received().Increment();
        await mailingService.DidNotReceive().SendMail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>());
        Assert.Single(messageBus.PublishedMessages);
    }

    [Fact]
    public async Task user_with_active_rental_cannot_rent_another_scooter()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterUser("email@lukaszreszke.pl", "password", "Łukasz");
        registrationService.RegisterNewScooter(0.5m);
          var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);
        var userId = context.Users.Last().Id;
        var scooterId = context.Scooters.Last().Id;
        await rentalService.RentScooter(userId, scooterId);

        // When
        var result = await rentalService.RentScooter(userId, scooterId);

        // Then
        Assert.False(result);
        Assert.Single(messageBus.PublishedMessages);
        await mailingService.DidNotReceive().SendMail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>());
        activeRentalsCounter.Received(1).Increment();
    }

    [Fact]
    public async Task ineligible_user_cannot_rent_scooter()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterUser("email@lukaszreszke.pl", "password", "Łukasz");
        registrationService.RegisterNewScooter(0.5m);
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var userId = context.Users.Last().Id;
        context.Users.Last().ViolationCount = 4;
        await context.SaveChangesAsync();
        var scooterId = context.Scooters.Last().Id;
        userManager.IsPremiumUser(userId).Returns(false);
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);

        // When
        var result = await rentalService.RentScooter(userId, scooterId);

        // Then
        Assert.False(result);
        Assert.Empty(messageBus.PublishedMessages);
        await mailingService.DidNotReceive().SendMail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>());
        activeRentalsCounter.DidNotReceive().Increment();
        activeRentalsCounter.DidNotReceive().Decrement();
    }

    [Fact]
    public async Task user_cannot_rent_nonexistent_scooter()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterUser("email@lukaszreszke.pl", "password", "Łukasz");
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var userId = context.Users.Last().Id;
        userManager.IsPremiumUser(userId).Returns(true);
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);

        // When
        var result = await rentalService.RentScooter(userId, 999);

        // Then
        Assert.False(result);
        Assert.Empty(messageBus.PublishedMessages);
        await mailingService.DidNotReceive().SendMail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>());
        activeRentalsCounter.DidNotReceive().Increment();
        activeRentalsCounter.DidNotReceive().Decrement();
    }

    [Fact]
    public async Task rental_can_be_finished()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterUser("email@lukaszreszke.pl", "password", "Łukasz");
        registrationService.RegisterNewScooter(0.5m);
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var userId = context.Users.Last().Id;
        userManager.IsPremiumUser(userId).Returns(true);
        var scooterId = context.Scooters.Last().Id;
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);

        await rentalService.RentScooter(userId, scooterId);

        // When
        var result = await rentalService.FinishRental(context.Rentals.Last().Id);

        // Then
        Assert.True(result);
        activeRentalsCounter.Received().Decrement();
        Assert.Equal(2, messageBus.PublishedMessages.Length);
        await mailingService.Received().SendMail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>());
    }

    [Fact]
    public async Task finished_rental_cannot_be_finished_again()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterUser("email@lukaszreszke.pl", "password", "Łukasz");
        registrationService.RegisterNewScooter(0.5m);
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);
        var userId = context.Users.Last().Id;
        var scooterId = context.Scooters.Last().Id;

        await rentalService.RentScooter(userId, scooterId);
        var rentalId = context.Rentals.Last().Id;
        await rentalService.FinishRental(rentalId);

        // When
        var result = await rentalService.FinishRental(rentalId);

        // Then
        Assert.False(result);
        activeRentalsCounter.Received(1).Decrement();
        Assert.Equal(2, messageBus.PublishedMessages.Length);
        await mailingService.Received(1).SendMail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>());
    }

    [Fact]
    public async Task show_offer_returns_correct_offer_for_premium_user()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterUser("email@lukaszreszke.pl", "password", "Łukasz");
        registrationService.RegisterNewScooter(0.5m);
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var userId = context.Users.Last().Id;
        userManager.IsPremiumUser(userId).Returns(true);
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);
        var scooterId = context.Scooters.Last().Id;

        // When
        var offer = await rentalService.ShowOffer(userId, scooterId);

        // Then
        Assert.Equal(0.4m, offer.PricePerMinute);
        Assert.Equal(context.Scooters.Last().Range, offer.Range);
        userManager.Received().IsPremiumUser(userId);
    }

    [Fact]
    public async Task show_offer_returns_empty_offer_for_nonexistent_scooter()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterUser("email@lukaszreszke.pl", "password", "Łukasz");
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);
        var userId = context.Users.Last().Id;

        // When
        var offer = await rentalService.ShowOffer(userId, 999);

        // Then
        Assert.Equal(0, offer.PricePerMinute);
        Assert.Equal(0, (int)offer.Range);
    }

    [Fact]
    public async Task send_scooter_for_maintenance_updates_scooter_status()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterNewScooter(0.5m);
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);
        var scooterId = context.Scooters.Last().Id;

        // When
        var result = await rentalService.SendScooterForMaintenance(scooterId, "Battery issue");

        // Then
        Assert.True(result);
        var scooter = await context.Scooters.FindAsync(scooterId);
        Assert.Equal("Maintenance", scooter.Status);
        Assert.True(scooter.IsUnderMaintenance);
        userManager.DidNotReceive().IsPremiumUser(Arg.Any<int>());
    }

    [Fact]
    public async Task send_scooter_for_maintenance_fails_for_nonexistent_scooter()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);

        // When
        var result = await rentalService.SendScooterForMaintenance(999, "Battery issue");

        // Then
        Assert.False(result);
        userManager.DidNotReceive().IsPremiumUser(Arg.Any<int>());
    }

    [Fact]
    public async Task complete_maintenance_updates_maintenance_record_and_scooter_status()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var registrationService = new RegistrationService(context);
        registrationService.RegisterNewScooter(0.5m);
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);
        var scooterId = context.Scooters.Last().Id;
        await rentalService.SendScooterForMaintenance(scooterId, "Battery issue");
        var maintenanceRecordId = context.MaintenanceRecords.Last().Id;

        // When
        var result = await rentalService.CompleteMaintenance(maintenanceRecordId);

        // Then
        Assert.True(result);
        var record = await context.MaintenanceRecords.FindAsync(maintenanceRecordId);
        Assert.True(record.IsCompleted);
        var scooter = await context.Scooters.FindAsync(scooterId);
        Assert.Equal("Available", scooter.Status);
        Assert.False(scooter.IsUnderMaintenance);
        userManager.DidNotReceive().IsPremiumUser(Arg.Any<int>());
    }

    [Fact]
    public async Task complete_maintenance_fails_for_nonexistent_record()
    {
        // Given
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseInMemoryDatabase(databaseName: "RentalServiceTests")
            .Options;

        await using var context = new RentalDbContext(options);
        var messageBus = new TestableMessageSession(); 
        var activeRentalsCounter = Substitute.For<IActiveRentalsCounter>();
        var mailingService = Substitute.For<IMailingService>();
        var userManager = Substitute.For<IUserManager>();
        var rentalService = new ScooterService(context, messageBus, activeRentalsCounter, mailingService, userManager);

        // When
        var result = await rentalService.CompleteMaintenance(999);

        // Then
        Assert.False(result);
        userManager.DidNotReceive().IsPremiumUser(Arg.Any<int>());
    }
}