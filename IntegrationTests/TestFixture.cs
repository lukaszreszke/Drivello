using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Loyaltello;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Polly;
using Respawn;
using Testcontainers.PostgreSql;
using Program = Drivello.Program;

namespace IntegrationTests;

public class TestFixture : IAsyncLifetime
{
    private NpgsqlConnection _dbConnection;
    public PostgreSqlContainer DrivelloDbContainer { get; set; }
    public PostgreSqlContainer LoyaltelloDbContainer { get; set; }
    public IContainer LoyaltelloContainer { get; set; }
    public DrivelloApplicationFactory DrivelloFactory { get; set; }
    public IConfiguration Configuration { get; set; }
    public HttpClient DrivelloClient { get; private set; }
    public INetwork Network { get; set; }

    public TestFixture()
    {
;
    }

    public IContainer RabbitMqContainer { get; set; }


    public async Task InitializeAsync()
    {
        Network = new NetworkBuilder()
            .WithName("drivello-network")
            .Build();

        DrivelloDbContainer = new PostgreSqlBuilder()
            .WithDatabase("drivello_test")
            .WithUsername("drivello_user")
            .WithPassword("drivello_password")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
        
        LoyaltelloDbContainer = new PostgreSqlBuilder()
            .WithDatabase("loyaltello_test")
            .WithNetwork(Network)
            .WithNetworkAliases("loyaltello-db")
            .WithPortBinding(5433, 5432)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
        
        RabbitMqContainer = new ContainerBuilder()
            .WithImage("rabbitmq:3.12-management")
            .WithNetwork(Network)
            .WithNetworkAliases("rabbitmq")
            .WithPortBinding(5672, 5672) 
            .WithPortBinding(15672, 15672) 
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
            .Build();
        
        var configDictionary = new Dictionary<string, string>
        {
            { "ConnectionStrings:DefaultConnection", "Host=localhost;Database=drivello_test;Username=drivello_user;Password=drivello_password" },
            { "RabbitMQ:ConnectionString", "host=localhost;username=guest;password=guest" },
            { "LoyaltyApiBaseUrl", "http://localhost:5001" }
        };

        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddInMemoryCollection(configDictionary)
            .Build();
            
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(
                Path.Combine(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath))
            .WithDockerfile("Loyaltello/Dockerfile")
            .WithCleanUp(true)
            .Build();

        await image.CreateAsync();

        var containerConnectionString =
            "Host=loyaltello-db;Database=loyaltello_test;Username=postgres;Password=postgres";
        LoyaltelloContainer = new ContainerBuilder()
            .WithImage(image)
            .WithNetwork(Network)
            .WithPortBinding(5001, 8080)
            .WithEnvironment("ConnectionStrings:DefaultConnection", containerConnectionString)
            .WithEnvironment("RabbitMQ:ConnectionString", "host=rabbitmq;username=guest;password=guest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .Build();

        await Network.CreateAsync();

        await Task.WhenAll(
            DrivelloDbContainer.StartAsync(),
            LoyaltelloDbContainer.StartAsync(),
            RabbitMqContainer.StartAsync()
        );

        await LoyaltelloContainer.StartAsync();

        DrivelloFactory = new DrivelloApplicationFactory(DrivelloDbContainer, Configuration, 5000);

        DrivelloClient = DrivelloFactory.CreateClient();

        await WaitForService(DrivelloClient, "/health");
        
        _dbConnection = new NpgsqlConnection(DrivelloDbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();
    }
    
    private async Task WaitForService(HttpClient client, string healthCheckEndpoint)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        await retryPolicy.ExecuteAsync(async () =>
        {
            var response = await client.GetAsync(healthCheckEndpoint);
            response.EnsureSuccessStatusCode();
        });
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            DrivelloDbContainer.DisposeAsync().AsTask(),
            LoyaltelloDbContainer.DisposeAsync().AsTask(),
            LoyaltelloContainer.DisposeAsync().AsTask(),
            Network.DisposeAsync().AsTask()
        );
    }
}