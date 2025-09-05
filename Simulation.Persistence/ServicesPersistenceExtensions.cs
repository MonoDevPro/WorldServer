using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulation.Application.Options;
using Simulation.Application.Ports.ECS.Utils.Indexers;
using Simulation.Persistence.Char;

namespace Simulation.Persistence;

public static class ServicesPersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        // Índice Espacial (usando a implementação com QuadTree)
        services.AddSingleton<ISpatialIndex, QuadTreeSpatial>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<SpatialOptions>>().Value;
            return new QuadTreeSpatial(options.MinX, options.MinY, options.Width, options.Height);
        });
        return services;
    }
}