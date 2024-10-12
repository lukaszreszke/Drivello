using Microsoft.EntityFrameworkCore;
using Endpoint = NServiceBus.Endpoint;

namespace Drivello.Infrastructure
{
    public static class NServiceBusConfiguration
    {
        public static EndpointConfiguration AddNServiceBus(
            this WebApplicationBuilder builder,
            IConfiguration configuration,
            string endpointName = "Drivello")
        {
            var endpointConfiguration = new EndpointConfiguration(endpointName);
            var scanner = endpointConfiguration.AssemblyScanner();
            scanner.ExcludeAssemblies(
                "xunit.runner.utility.netcoreapp10.dll",
                "xunit.runner.visualstudio.testadapter.dll",
                "xunit.runner.reporters.netcoreapp10.dll",
                "Loyaltello.dll"
            );
            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
            transport.UseConventionalRoutingTopology(QueueType.Classic);
            transport.ConnectionString(configuration.GetValue<string>("RabbitMQ:ConnectionString"));

            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();

            return endpointConfiguration;
        }
    }
}