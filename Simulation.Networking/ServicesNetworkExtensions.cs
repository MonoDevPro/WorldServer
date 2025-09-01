using LiteNetLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.Options;
using Simulation.Application.Ports;
using Simulation.Application.Ports.Char;

namespace Simulation.Networking;

public static class ServicesNetworkExtensions
{
    public static IServiceCollection AddSimulationNetwork(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<NetworkOptions>()
            .Bind(configuration.GetSection(NetworkOptions.SectionName));
        
        // Regista a classe unificada como ela própria e também como a implementação da interface.
        services.AddSingleton<LiteNetServer>();
        services.AddSingleton<ICharSnapshotPublisher>(sp => sp.GetRequiredService<LiteNetServer>());
        
        // O NetPacketProcessor é agora uma dependência do LiteNetServer, então registamo-lo.
        services.AddSingleton<NetPacketProcessor>();

        return services;
    }
}