using LoyaltyApi.Tests.Pacts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Verifier;
using ProviderTests;
using Shared.Messages;
using Xunit.Abstractions;

namespace ContractTests;

public class ProviderTests : IDisposable
{
    private static readonly Uri ProviderUri = new("http://localhost:5144");

    private readonly IHost _server;
    private readonly PactVerifier _verifier;

    public ProviderTests(ITestOutputHelper output)
    {
        _server = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls(ProviderUri.ToString());
                webBuilder.UseStartup<TestStartup>();
            })
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "UseNServiceBus", "false" }
                });
            })
            .Build();

        _server.Start();
        _verifier = new PactVerifier("Loyalty API", new PactVerifierConfig
        {
            LogLevel = PactLogLevel.Debug,
            Outputters = new List<IOutput>
            {
                new XUnitOutput(output)
            }
        });
    }


    [Fact]
    public void Verify()
    {
    }

    public void Dispose()
    {
        _server.Dispose();
        _verifier.Dispose();
    }
}