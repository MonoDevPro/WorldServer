using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons;
using Simulation.Domain.Templates;

namespace Simulation.Pooling;

public static class ServicesPoolingExtensions
{
    /// <summary>
    /// Método principal que registra todos os serviços de pooling necessários para a aplicação.
    /// </summary>
    public static IServiceCollection AddPoolingServices(this IServiceCollection services)
    {
        // (opcional) registrar uma facade que usa estes pools (ver seção 3)
        services.AddSingleton<IPoolsService, PoolsService>();
        
        // provider padrão para criar ObjectPool<T>
        services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        
        // pool para List<CharTemplate>
        services.AddSingleton<ObjectPool<List<PlayerTemplate>>>(sp =>
        {
            var provider = sp.GetRequiredService<ObjectPoolProvider>();
            var policy = new PooledListPolicy<PlayerTemplate>(maxAllowedCapacity: 2048);
            // você pode ajustar maximumRetained via DefaultObjectPool<T> ctor se desejar
            return provider.Create(policy);
        });
        
        // pool para CharTemplate
        services.AddSingleton<ObjectPool<PlayerTemplate>>(sp =>
        {
            var provider = sp.GetRequiredService<ObjectPoolProvider>();
            var policy = new CharTemplatePooledPolicy();
            return provider.Create(policy);
        });
        
        // ArrayPool é singleton compartilhado
        services.AddSingleton<ArrayPool<PlayerTemplate>>(sp => ArrayPool<PlayerTemplate>.Shared);
        
        return services;
    }
}

