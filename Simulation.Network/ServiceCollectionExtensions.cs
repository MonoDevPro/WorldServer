using Microsoft.Extensions.DependencyInjection;

namespace Simulation.Network;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimulationNetwork(this IServiceCollection services)
    {
        services.AddSingleton<NetworkSystem>();
        return services;
    }
}
