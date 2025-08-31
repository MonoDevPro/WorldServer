using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Client.Core;
using Simulation.Client.Network;
using Simulation.Client.Systems;
using Simulation.Network;

namespace Simulation.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimulationClient(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurações
        services.Configure<NetworkOptions>(configuration.GetSection(NetworkOptions.SectionName));
        
        // Mundo ECS do cliente
        services.AddSingleton<World>(_ => World.Create());
        
        // Sistemas ECS
        services.AddSingleton<SnapshotHandlerSystem>();
        
        // Rede
        services.AddSingleton<LiteNetClient>();
        
        // Registra as interfaces usando as implementações
        services.AddSingleton<ISnapshotHandler>(provider => provider.GetRequiredService<SnapshotHandlerSystem>());
        services.AddSingleton<IIntentSender>(provider => provider.GetRequiredService<LiteNetClient>());
        
        // Gerenciamento de entrada
        services.AddSingleton<InputManager>();
        
        return services;
    }
}