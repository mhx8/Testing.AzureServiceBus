using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Testing.AzureServiceBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureServiceBusEmulator(
        this IServiceCollection services,
        Action<ServiceBusEmulatorBuilder> builder)
    {
        services.AddAzureClients(
            clientBuilder =>
            {
                clientBuilder.AddServiceBusClient(ServiceBusConstants.ConnectionString)
                    .WithName(ServiceBusConstants.LocalServiceBusClientName);
            });

        builder(new ServiceBusEmulatorBuilder(services));

        return services;
    }
}