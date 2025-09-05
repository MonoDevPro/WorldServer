using Arch.Buffer;
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
using Simulation.Application.Services.Handler;
using Simulation.Application.Services.Publisher;
using Simulation.Application.Systems;
using Simulation.Factories;
using Simulation.Networking;
using Simulation.Persistence;
using Simulation.Pooling;

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
        services.AddFactories();
        services.AddPoolingServices();
        services.AddSingleton<DoubleBufferedCommandBuffer>();

        // Registra os serviços específicos da aplicação Console
        services.AddSingleton<PerformanceMonitor>();
        services.AddSingleton<ServerLoop>();
        services.AddSingleton<IMapSnapshotPublisher, MapLoaderHandler>();
        
        // --- Núcleo do ECS ---
        // Registra o World como um singleton. Cada sistema receberá a mesma instância.
        services.AddSingleton<World>(provider => WorldFactory.Create());
        
        services.AddSingleton<IntentForwarding>();
        services.AddSingleton<ICharIntentHandler>(sp => sp.GetRequiredService<IntentForwarding>());
        services.AddSingleton<IMapIntentHandler>(sp => sp.GetRequiredService<IntentForwarding>());
        
        // --- Sistemas da Simulação ---
        // A ordem de registro deve ser respeitada, pois define a ordem de execução no pipeline.
        // Intenções de Entrada
        services.AddSingleton<ISystem<float>, ProcessIntentsSystem>();
        // Lógica de Jogo
        services.AddSingleton<ISystem<float>, MapLifecycleSystem>();
        services.AddSingleton<ISystem<float>, CharLifecycleSystem>();
        services.AddSingleton<ISystem<float>, GridMovementSystem>();
        services.AddSingleton<ISystem<float>, TeleportSystem>();
        services.AddSingleton<ISystem<float>, AttackSystem>();
        services.AddSingleton<ISystem<float>, LifetimeSystem>();
        services.AddSingleton<ISystem<float>, SpatialIndexSyncSystem>();
        services.AddSingleton<ISystem<float>, SnapshotForwarding>();

        // --- Pipeline e Runner ---
        // O SimulationPipeline irá injetar todos os sistemas registrados acima na ordem correta.
        services.AddSingleton<SimulationPipeline>();
        services.AddSingleton<SimulationRunner>();

        return services;
    }
}