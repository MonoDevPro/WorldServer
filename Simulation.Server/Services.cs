using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Application.Factories;
using Simulation.Application.Options;
using Simulation.Application.Ports;
using Simulation.Application.Ports.Map;
using Simulation.Application.Services;
using Simulation.Application.Systems;
using Simulation.Networking;
using Simulation.Persistence;

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
        services.AddSingleton<IMapLoaderService, MapLoaderService>();
        services.AddSingleton<ServerLoop>();
        
        
        // --- Núcleo do ECS ---
        // Registra o World como um singleton. Cada sistema receberá a mesma instância.
        services.AddSingleton<World>(provider => WorldFactory.Create());

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