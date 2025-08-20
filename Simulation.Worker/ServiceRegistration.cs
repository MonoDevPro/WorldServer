using Simulation.Core.Utilities;

namespace Simulation.Worker;

public static class ServiceRegistration
{
    public static IServiceCollection AddSimulation(this IServiceCollection services)
    {
        services.AddSingleton<IWorldFactory, DefaultWorldFactory>();
        return services;
    }
}