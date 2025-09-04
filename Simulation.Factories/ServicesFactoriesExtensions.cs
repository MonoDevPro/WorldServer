using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.Ports.Char.Factories;
using Simulation.Application.Ports.Commons.Factories;
using Simulation.Application.Ports.Map.Factories;
using Simulation.Domain.Templates;

namespace Simulation.Factories;

public static class ServicesFactoriesExtensions
{
    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        services.AddSingleton<CharFactory>();
        services.AddSingleton<ICharFactory>(provider => provider.GetRequiredService<CharFactory>());
        services.AddSingleton<IFactory<Entity, CharTemplate>>(provider => provider.GetRequiredService<CharFactory>());
        services.AddSingleton<IArchetypeProvider<CharTemplate>>(provider => provider.GetRequiredService<CharFactory>());
        services.AddSingleton<IQueryProvider<CharTemplate>>(provider => provider.GetRequiredService<CharFactory>());
        
        services.AddSingleton<MapFactory>();
        services.AddSingleton<IMapFactory>(provider => provider.GetRequiredService<MapFactory>());
        services.AddSingleton<IFactory<Entity, MapTemplate>>(provider => provider.GetRequiredService<MapFactory>());
        services.AddSingleton<IArchetypeProvider<MapTemplate>>(provider => provider.GetRequiredService<MapFactory>());
        services.AddSingleton<IQueryProvider<MapTemplate>>(provider => provider.GetRequiredService<MapFactory>());
        
        return services;
    }
}