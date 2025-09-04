using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons.Pools;
using Simulation.Domain.Templates;

namespace Simulation.Pooling;

public static class ServicesPoolingExtensions
{
    /// <summary>
    /// Método principal que registra todos os serviços de pooling necessários para a aplicação.
    /// </summary>
    public static IServiceCollection AddPoolingServices(this IServiceCollection services)
    {
        // --- Registro de Políticas ---
        // 1. Registra a política genérica para objetos que implementam IResetable.
        services.AddSingleton(typeof(IPooledObjectPolicy<>), typeof(ResetableObjectPolicy<>));

        // 2. Registra a política específica para CharTemplate, pois ele não é IResetable.
        services.AddSingleton<IPooledObjectPolicy<CharTemplate>, CharTemplatePolicy>();

        // --- Registro de Pools de Objeto (IObjectPool<T>) ---
        // 3. Usa o método auxiliar para os tipos que são IResetable.
        services.AddPoolFor<CharSaveTemplate>();
        
        // 4. Usa o método auxiliar para pools de List<T>.
        services.AddPoolForList<CharTemplate>();

        // 5. Registro manual para IObjectPool<CharTemplate> usando sua política específica.
        services.AddSingleton<ObjectPool<CharTemplate>>(provider =>
        {
            var policy = provider.GetRequiredService<IPooledObjectPolicy<CharTemplate>>();
            return new DefaultObjectPool<CharTemplate>(policy);
        });
        services.AddSingleton<IObjectPool<CharTemplate>, MicrosoftObjectPoolAdapter<CharTemplate>>();

        // --- Registro de Pools de Array (IArrayPool<T>) ---
        // 6. Registra o adaptador genérico para IArrayPool<T>.
        services.AddSingleton(typeof(IArrayPool<>), typeof(DefaultArrayPoolAdapter<>));
        
        return services;
    }

    /// <summary>
    /// Método de extensão genérico que registra as dependências para um pool de objetos
    /// que implementam IResetable.
    /// </summary>
    private static IServiceCollection AddPoolFor<T>(this IServiceCollection services) 
        where T : class, IResetable, new()
    {
        services.AddSingleton<ObjectPool<T>>(provider =>
        {
            var policy = provider.GetRequiredService<IPooledObjectPolicy<T>>();
            return new DefaultObjectPool<T>(policy);
        });
        services.AddSingleton<IObjectPool<T>, MicrosoftObjectPoolAdapter<T>>();
        return services;
    }
    
    /// <summary>
    /// Método de extensão genérico que registra as dependências para um pool de objetos List<T>.
    /// </summary>
    private static IServiceCollection AddPoolForList<T>(this IServiceCollection services)
    {
        services.AddSingleton<ObjectPool<List<T>>>(provider =>
        {
            // (CORREÇÃO) Cria a política diretamente, pois ela não tem dependências.
            // Isso evita o registro DI complexo e o erro de compilação.
            var policy = new ListClearPolicy<T>();
            return new DefaultObjectPool<List<T>>(policy);
        });
        services.AddSingleton<IObjectPool<List<T>>, MicrosoftObjectPoolAdapter<List<T>>>();
        return services;
    }
}

