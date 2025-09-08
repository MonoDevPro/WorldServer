using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Application;
using Simulation.Application.Options;
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
        
        services.AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")).AddConsole());
        
        services.AddApplication()
            .ConfigureOptions<ServerOptions>(configuration, ServerOptions.SectionName)
            .ConfigureOptions<GameLoopOptions>(configuration, GameLoopOptions.SectionName)
            .ConfigureOptions<NetworkOptions>(configuration, GameLoopOptions.SectionName)
            .ConfigureOptions<SpatialOptions>(configuration, GameLoopOptions.SectionName)
            .ConfigureOptions<WorldOptions>(configuration, GameLoopOptions.SectionName)
            .AddPerformanceMonitor();

        services.AddPersistence(configuration);
        
        return services;
    }
}