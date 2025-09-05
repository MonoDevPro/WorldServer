using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Application;
using Simulation.Application.Options;
using Simulation.Application.Ports.ECS.Publishers;
using Simulation.Application.Services;
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
        
        services.Configure<NetworkOptions>(configuration.GetSection(NetworkOptions.SectionName));
        services.Configure<SpatialOptions>(configuration.GetSection(SpatialOptions.SectionName));
        services.Configure<WorldOptions>(configuration.GetSection(WorldOptions.SectionName));
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

        services.AddApplication(configuration);
        services.AddPersistence();
        services.AddNetwork();
        services.AddPoolingServices();

        // Registra os serviços específicos da aplicação Console
        services.AddSingleton<PerformanceMonitor>();
        services.AddSingleton<ServerLoop>();

        return services;
    }
}