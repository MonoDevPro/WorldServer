using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.Options;
using Simulation.Application.Ports;
using Simulation.Application.Ports.ECS.Publishers;

namespace Simulation.Networking;

public static class ServicesNetworkExtensions
{
    public static IServiceCollection AddNetwork(this IServiceCollection services)
    {
        services.AddSingleton<LiteNetLibAdapter>();
        services.AddSingleton<IPlayerSnapshotPublisher>(sp => sp.GetRequiredService<LiteNetLibAdapter>());
        services.AddSingleton<IMapSnapshotPublisher>(sp => sp.GetRequiredService<LiteNetLibAdapter>());
        services.AddSingleton<INetEventListener>(sp => sp.GetRequiredService<LiteNetLibAdapter>());
        
        return services;
    }
}