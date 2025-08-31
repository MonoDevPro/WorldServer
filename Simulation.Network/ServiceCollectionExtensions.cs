using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Network;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimulationNetwork(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Regista o netoptions e o associa à secção "net" do appsettings.json
        services.AddOptions<NetworkOptions>()
            .Bind(configuration.GetSection(NetworkOptions.SectionName));
        
        // Registra o LiteNetServer, que gerencia a conexão
        services.AddSingleton<LiteNetServer>();
        services.AddSingleton<NetPacketProcessor>();
        
        // Registra a implementação do Publisher.
        // O ISnapshotPublisher é a "Porta", e LiteNetPublisher é o "Adaptador".
        services.AddSingleton<ISnapshotPublisher, LiteNetPublisher>();

        return services;
    }
}