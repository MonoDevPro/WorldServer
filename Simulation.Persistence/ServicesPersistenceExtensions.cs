using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulation.Application.Factories;
using Simulation.Application.Options;
using Simulation.Application.Ports;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Index;
using Simulation.Application.Ports.Map;
using Simulation.Application.Services;
using Simulation.Application.Systems;
using Simulation.Core;
using Simulation.Core.Systems;

namespace Simulation.Persistence;

public static class ServicesPersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Regista o WorldOptions e o associa à secção "World" do appsettings.json
        services.AddOptions<WorldOptions>()
            .Bind(configuration.GetSection(WorldOptions.SectionName));
        
        // Índice Espacial (usando a implementação com QuadTree)
        services.AddSingleton<ISpatialIndex, QuadTreeIndex>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<WorldOptions>>().Value;
            return new QuadTreeIndex(options.MinX, options.MinY, options.Width, options.Height);
        });
        
        // Índice de Personagens (CharId -> Entity)
        services.AddSingleton<ICharIndex, CharIndex>();
        
        // Índice de Mapas (MapId -> MapData)
        services.AddSingleton<IMapIndex, MapIndex>();
        
        // Repositório de Templates (simulando acesso a dados)
        services.AddSingleton<ICharTemplateRepository, InMemoryCharTemplateRepository>();
        
        return services;
    }
}