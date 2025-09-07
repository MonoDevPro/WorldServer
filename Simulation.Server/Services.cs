using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Network.Adapters;
using Simulation.Application;
using Simulation.Application.Options;
using Simulation.Application.Services;
using Simulation.Application.Services.Loop;
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
        services.AddSingleton<NetworkOptions>(sp => 
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NetworkOptions>>().Value);
        services.Configure<SpatialOptions>(configuration.GetSection(SpatialOptions.SectionName));
        services.AddSingleton<SpatialOptions>(sp => 
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SpatialOptions>>().Value);
        services.Configure<WorldOptions>(configuration.GetSection(WorldOptions.SectionName));
        services.AddSingleton<WorldOptions>(sp => 
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WorldOptions>>().Value);
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

        services.AddApplication(configuration);
        services.AddPersistence();
        services.AddPoolingServices();
        services.AddServerNetworking();
        
        services.AddSingleton<GameLoop>();
        services.TryAddSingleton<PerformanceMonitor>();


        // Registra os serviços específicos da aplicação Console

        return services;
    }
}