using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Application.Factories;
using Simulation.Application.Options;
using Simulation.Application.Ports;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Map;
using Simulation.Application.Services;
using Simulation.Application.Systems;
using Simulation.Application.Systems.In;
using Simulation.Application.Systems.Out;
using Simulation.Networking;
using Simulation.Persistence;
using Simulation.Persistence.Map;
using CharSnapshotPublisherSystem = Simulation.Application.Systems.Out.CharSnapshotPublisherSystem;

namespace Simulation.Server;

public static class Services
{
    public static IServiceCollection AddServerServices(this IServiceCollection services)
    {
        // 1. Construção da Configuração
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

        // Registra as opções de configuração
        services.Configure<NetworkOptions>(configuration.GetSection(NetworkOptions.SectionName));
        services.Configure<WorldOptions>(configuration.GetSection(WorldOptions.SectionName));

        // Adiciona todos os serviços dos projetos Core e Network
        services.AddPersistence(configuration);
        services.AddSimulationNetwork(configuration);

        // Registra os serviços específicos da aplicação Console
        services.AddSingleton<PerformanceMonitor>();
        services.AddSingleton<ServerLoop>();
        
        
        // --- Núcleo do ECS ---
        // Registra o World como um singleton. Cada sistema receberá a mesma instância.
        services.AddSingleton<World>(provider => WorldFactory.Create());
        
        services.AddSingleton<MapIntentHandlerSystem>();
        services.AddSingleton<CharIntentsHandlerSystem>();
        services.AddSingleton<MapSnapshotPublisherSystem>();
        services.AddSingleton<CharSnapshotPublisherSystem>();
        services.AddSingleton<IMapSnapshotPublisher, MapLoaderHandler>();
        services.AddSingleton<IMapIntentHandler>(sp => sp.GetRequiredService<MapIntentHandlerSystem>());
        services.AddSingleton<ICharIntentHandler>(sp => sp.GetRequiredService<CharIntentsHandlerSystem>());
        
        // --- Sistemas da Simulação ---
        // A ordem de registro deve ser respeitada, pois define a ordem de execução no pipeline.
        // Intenções de Entrada
        services.AddSingleton<ISystem<float>>( p => p.GetRequiredService<MapIntentHandlerSystem>());
        services.AddSingleton<ISystem<float>>( p => p.GetRequiredService<CharIntentsHandlerSystem>());
        // Lógica de Jogo
        services.AddSingleton<ISystem<float>, PlayerLifecycleSystem>();
        services.AddSingleton<ISystem<float>, GridMovementSystem>();
        services.AddSingleton<ISystem<float>, TeleportSystem>();
        services.AddSingleton<ISystem<float>, AttackSystem>();
        services.AddSingleton<ISystem<float>, LifetimeSystem>();
        services.AddSingleton<ISystem<float>, SpatialIndexSyncSystem>();
        // Publicadores de Estado (Snapshots)
        services.AddSingleton<ISystem<float>, TemplateSyncSystem>();
        services.AddSingleton<ISystem<float>, MapSnapshotPublisherSystem>();
        services.AddSingleton<ISystem<float>, CharSnapshotPublisherSystem>();

        // --- Pipeline e Runner ---
        // O SimulationPipeline irá injetar todos os sistemas registrados acima na ordem correta.
        services.AddSingleton<SimulationPipeline>();
        services.AddSingleton<SimulationRunner>();

        return services;
    }
}