using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Network.Adapters.LiteNet;
using Network.Adapters.Serialization;
using Simulation.Application.Ports.ECS.Publishers;
using Simulation.Application.Ports.Network;
using Simulation.Application.Ports.Network.Inbound;
using Simulation.Application.Ports.Network.Outbound;
using Simulation.Networking.Services;

namespace Simulation.Networking;

public static class ServicesNetworkExtensions
{
    
    public static IServiceCollection AddClientNetworking(this IServiceCollection services)
    {
        services.RegisterCommonServices();
        
        // Aplicações -> Portas de entrada
        services.AddSingleton<IClientNetworkApp, ClientNetworkApp>();
        
        // Serviços de rede // Portas de entrada
        services.AddSingleton<IClientNetworkService, LiteNetLibClientAdapter>();
        
        return services;
    }
    
    public static IServiceCollection AddServerNetworking(this IServiceCollection services)
    {
        services.RegisterCommonServices();
        
        // Aplicações -> Portas de entrada
        services.AddSingleton<IServerNetworkApp, ServerApp>();
        
        // Serviços de rede // Portas de entrada
        services.AddSingleton<IServerNetworkService, LiteNetLibServerAdapter>();
        
        services.AddSingleton<IPlayerSnapshotPublisher, SnapshotPublisher>();
        
        return services;
    }
    
    private static IServiceCollection RegisterCommonServices(this IServiceCollection services)
    {
        services.AddSingleton<IPlayerConnectionMap, PeerCharMap>();
        services.TryAddSingleton<INetworkEventBus, NetworkEventBus>();
        services.TryAddSingleton<INetworkSerializer, SerializerAdapter>();
        services.TryAddSingleton<LiteNetLibConnectionManagerAdapter>();
        services.TryAddSingleton<IConnectionManager>(sp => sp.GetRequiredService<LiteNetLibConnectionManagerAdapter>());
        services.TryAddSingleton<LiteNetLibPacketHandlerAdapter>();
        services.TryAddSingleton<IPacketSender, LiteNetLibPacketSenderAdapter>();
        services.TryAddSingleton<IPacketRegistry, LiteNetLibPacketRegistryAdapter>();
        
        return services;
    }
}