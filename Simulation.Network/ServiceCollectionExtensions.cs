using Microsoft.Extensions.DependencyInjection;

namespace Simulation.Network;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimulationNetwork(this IServiceCollection services)
    {
        services.AddHostedService<LiteNetServer>();
        return services;
    }
}
