using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Adapters.Index;
using Simulation.Core.Abstractions.Adapters.Map;
using Simulation.Core.Abstractions.Adapters.Spatial;
using Simulation.Core.Abstractions.Ports;
using Simulation.Core.Abstractions.Ports.Index;
using Simulation.Core.Abstractions.Ports.Map;
using Simulation.Core.Adapters;
using Simulation.Core.Systems;

namespace Simulation.Core;

public static class Services
{
    public static IServiceCollection AddSimulationCore(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Regista o WorldOptions e o associa à secção "World" do appsettings.json
        services.AddOptions<WorldOptions>()
            .Bind(configuration.GetSection(WorldOptions.SectionName));
        
        // --- Núcleo do ECS ---
        // Registra o World como um singleton. Cada sistema receberá a mesma instância.
        services.AddSingleton<World>(provider => WorldFactory.Create());

        // --- Registros de Índices e Repositórios (Ports & Adapters) ---
        // Para cada interface (Port), registramos uma implementação concreta (Adapter).
        
        // Índice de Personagens (CharId -> Entity)
        services.AddSingleton<ICharIndex, CharIndex>();
        
        // Índice de Mapas (MapId -> MapData)
        services.AddSingleton<IMapIndex, MapIndex>();
        
        // Índice Espacial (usando a implementação com QuadTree)
        services.AddSingleton<ISpatialIndex, QuadTreeIndex>();
        
        // Repositório de Templates (simulando acesso a dados)
        services.AddSingleton<ICharTemplateRepository, InMemoryCharTemplateRepository>();

        // --- Sistemas da Simulação ---
        // A ordem de registro como singleton não importa, mas a ordem de execução no pipeline sim.

        // Sistemas de Entrada/Saída
        services.AddSingleton<IntentsHandlerSystem>();
        services.AddSingleton<IIntentHandler>(sp => sp.GetRequiredService<IntentsHandlerSystem>());
        
        services.AddSingleton<MapLoaderSystem>();
        services.AddSingleton<IMapLoaderSystem>(sp => sp.GetRequiredService<MapLoaderSystem>());
        
        services.AddSingleton<SnapshotPublisherSystem>();

        // Sistemas de Lógica de Jogo
        services.AddSingleton<PlayerLifecycleSystem>();
        services.AddSingleton<LifetimeSystem>(); // Para entidades temporárias
        services.AddSingleton<GridMovementSystem>();
        services.AddSingleton<TeleportSystem>();
        services.AddSingleton<AttackSystem>();
        
        // Sistemas de Sincronização e Finalização
        services.AddSingleton<SpatialIndexSyncSystem>();

        // --- Pipeline e Runner ---
        // O SimulationPipeline irá injetar todos os sistemas registrados acima na ordem correta.
        services.AddSingleton<SimulationPipeline>();
        services.AddSingleton<SimulationRunner>();

        return services;
    }
}

