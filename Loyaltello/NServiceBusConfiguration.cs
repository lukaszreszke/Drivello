using Loyaltello.EventHandlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Endpoint = NServiceBus.Endpoint;

namespace Loyaltello
{
    public static class NServiceBusConfiguration
    {
        public static IServiceCollection AddNServiceBus(
            this IServiceCollection services,
            IConfiguration configuration,
            string endpointName = "Loyaltello")
        {
            var endpointConfiguration = new EndpointConfiguration(endpointName);
            endpointConfiguration.AssemblyScanner().ExcludeAssemblies(
                "xunit.runner.utility.netcoreapp10.dll",
                "xunit.runner.visualstudio.testadapter.dll",
                "xunit.runner.reporters.netcoreapp10.dll"
            );

            endpointConfiguration.RegisterComponents(registration =>
            {
                registration.AddDbContext<LoyaltyDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            });
            
            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
            transport.UseConventionalRoutingTopology(QueueType.Classic);
            transport.ConnectionString(configuration.GetValue<string>("RabbitMQ:ConnectionString"));
            
            var conventions = endpointConfiguration.Conventions();
            conventions.DefiningEventsAs(type => type.Namespace == "Drivello.Models.Events" || typeof(IEvent).IsAssignableFrom(type));
            
            
            
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();

            var endpointInstance = Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();
            services.AddSingleton<IMessageSession>(endpointInstance);
            
            return services;
        }
    }
}