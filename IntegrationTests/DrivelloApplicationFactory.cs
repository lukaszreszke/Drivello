using Drivello;
using Drivello.Infrastructure;
using Drivello.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace IntegrationTests
{
    public class DrivelloApplicationFactory : WebApplicationFactory<Program> 
    {
        private readonly PostgreSqlContainer _dbContainer;
        private readonly IConfiguration _configuration;
        private readonly int _port;
        private NpgsqlConnection _dbConnection;

        public DrivelloApplicationFactory(PostgreSqlContainer dbContainer, IConfiguration configuration, int port)
        {
            _dbContainer = dbContainer;
            _configuration = configuration;
            _port = port;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls($"http://localhost:{_port}")
                .ConfigureKestrel(kestrel => { kestrel.ListenLocalhost(_port); })
                .ConfigureAppConfiguration(configBuilder => { configBuilder.AddConfiguration(_configuration); })
                .UseConfiguration(_configuration)
                .ConfigureTestServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<RentalDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    
                    services.AddDbContext<RentalDbContext>(options =>
                    {
                        options.UseNpgsql(_dbContainer.GetConnectionString());
                    });

                    var db = services.BuildServiceProvider().GetRequiredService<RentalDbContext>();
                    db.Database.EnsureCreated();

                    services.AddHttpClient<ILoyaltyService, LoyaltyServiceClient>(client =>
                    {
                        client.BaseAddress = new Uri(_configuration["LoyaltyApiBaseUrl"]);
                    });

                    services.AddHealthChecks()
                        .AddNpgSql(_dbContainer.GetConnectionString());
                });

            base.ConfigureWebHost(builder);
        }

        public T GetScopedService<T>() where T : class
        {
            var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<T>();
        }
    }
}