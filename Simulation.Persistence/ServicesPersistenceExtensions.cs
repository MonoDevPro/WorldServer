using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulation.Application.Options;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Application.Ports.Commons.Persistence;
using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Indexers;
using Simulation.Persistence.Char;
using Simulation.Persistence.Map;

namespace Simulation.Persistence;

public static class ServicesPersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Regista o WorldOptions e o associa à secção "World" do appsettings.json
        services.AddOptions<WorldOptions>()
            .Bind(configuration.GetSection(WorldOptions.SectionName));
        
        // Índice Espacial (usando a implementação com QuadTree)
        services.AddSingleton<ISpatialIndex, QuadTreeSpatial>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<WorldOptions>>().Value;
            return new QuadTreeSpatial(options.MinX, options.MinY, options.Width, options.Height);
        });
        
        // Mapas
        services.AddSingleton<IMapIndex, MapIndex>();                               // Índice de Mapas (MapId -> MapService)
        services.AddSingleton<IMapServiceIndex, SpatialMapIndex>();                               // Índice de Mapas (MapId -> MapService)
        services.AddSingleton<MapTemplateRepository>();     // Repositório de Templates banco em memória (simulando um banco de dados)
        services.AddSingleton<IMapTemplateRepository>(provider => provider.GetRequiredService<MapTemplateRepository>());
        services.AddSingleton<IInitializable>(provider => provider.GetRequiredService<MapTemplateRepository>());
        
        // Personagens
        services.AddSingleton<ICharIndex, CharIndex>();                             // Índice de Personagens (CharId -> Entity)
        services.AddSingleton<CharTemplateRepository>();   // Repositório de Templates banco em memória (simulando um banco de dados)
        services.AddSingleton<ICharTemplateRepository>(provider => provider.GetRequiredService<CharTemplateRepository>());
        services.AddSingleton<IInitializable>(provider => provider.GetRequiredService<CharTemplateRepository>());
        
        return services;
    }
}