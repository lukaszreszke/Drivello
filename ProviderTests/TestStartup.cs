using Loyaltello;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Testing;

namespace LoyaltyApi.Tests.Pacts;

public class TestStartup
{
    private readonly Startup inner;

    public TestStartup(IConfiguration configuration)
    {
        this.inner = new Startup(configuration);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<LoyaltyDbContext>(opts => opts.UseInMemoryDatabase("LoyaltyDb"));
        services.AddSingleton<IMessageSession>(new TestableMessageSession());
        
        this.inner.ConfigureServices(services);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseMiddleware<PactProviderStateMiddleware>();

        this.inner.Configure(app, env);
    }
}