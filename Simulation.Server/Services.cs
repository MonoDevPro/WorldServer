using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Application.Options;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Map;
using Simulation.Application.Services;
using Simulation.Application.Services.Handlers;
using Simulation.Application.Services.Publishers;
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
        services.AddPoolingServices();
        services.AddFactories();
        
        // Registra UMA ÚNICA instância do CommandBuffer para toda a aplicação.
        services.AddSingleton(new CommandBuffer());

        // Registra os serviços específicos da aplicação Console
        services.AddSingleton<PerformanceMonitor>();
        services.AddSingleton<ServerLoop>();
        
        
        // --- Núcleo do ECS ---
        // Registra o World como um singleton. Cada sistema receberá a mesma instância.
        services.AddSingleton<World>(provider => WorldFactory.Create(
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<WorldOptions>>().Value));
        
        services.AddSingleton<IntentForwarding>();
        services.AddSingleton<SnapshotForwarding>();
        services.AddSingleton<IMapSnapshotPublisher, MapLoaderHandler>();
        services.AddSingleton<IMapIntentHandler>(sp => sp.GetRequiredService<IntentForwarding>());
        services.AddSingleton<ICharIntentHandler>(sp => sp.GetRequiredService<IntentForwarding>());
        
        // --- Sistemas da Simulação ---
        // A ordem de registro deve ser respeitada, pois define a ordem de execução no pipeline.
        
        // Intenções de Entrada
        services.AddSingleton<ISystem<float>, ProcessIntentsSystem>();
        // Lógica de Jogo
        services.AddSingleton<ISystem<float>, MapLifecycleSystem>();
        services.AddSingleton<ISystem<float>, CharIndexSystem>();
        services.AddSingleton<ISystem<float>, CharSaveSystem>();
        services.AddSingleton<ISystem<float>, CharLifecycleSystem>();
        services.AddSingleton<ISystem<float>, GridMovementSystem>();
        services.AddSingleton<ISystem<float>, TeleportSystem>();
        services.AddSingleton<ISystem<float>, AttackSystem>();
        services.AddSingleton<ISystem<float>, LifetimeSystem>();
        services.AddSingleton<ISystem<float>, SpatialIndexSyncSystem>();
        
        // --- Pipeline e Runner ---
        // O SimulationPipeline irá injetar todos os sistemas registrados acima na ordem correta.
        services.AddSingleton<SimulationPipeline>();
        services.AddSingleton<SimulationRunner>();

        return services;
    }
}