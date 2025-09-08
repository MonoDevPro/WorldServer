using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.Ports.Loop;
using Simulation.Application.Ports.Pool;
using Simulation.Application.Services.Loop;
using Simulation.Application.Services.Pooling;

namespace Simulation.Application;

public static class ApplicationServices
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPool<List<Entity>>>( p => 
            new DefaultObjectPool<List<Entity>>(
                factory: () => new List<Entity>(), 
                reset: s => s.Clear(), 
                maxRetained: 3));
        
        services.AddSingleton<GameLoop>();
        
        return services;
    }
    
    public static IServiceCollection AddPerformanceMonitor(this IServiceCollection services)
    {
        services.AddSingleton<PerformanceMonitor>();
        return services;
    }
    
    public static IServiceCollection ConfigureOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class, new()
    {
        services.Configure<TOptions>(configuration.GetSection(sectionName));
        services.AddSingleton<TOptions>(sp => 
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TOptions>>().Value);
        return services;
    }
    
    public static IServiceCollection AddGameService<T>(this IServiceCollection services) 
        where T : class, IInitializable, IUpdatable
    {
        services.AddSingleton<T>();
        services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<T>());
        services.AddSingleton<IUpdatable>(sp => sp.GetRequiredService<T>());
        return services;
    }
}