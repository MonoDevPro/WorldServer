using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.Application.Ports.Persistence;
using Simulation.Application.Services;
using Simulation.Persistence.Repositories;
using Simulation.Persistence.Utils;

namespace Simulation.Persistence;

public static class ServicesPersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // 1. Adiciona o DbContext ao contêiner de serviços
        // O tempo de vida padrão aqui é 'Scoped' (uma instância por requisição HTTP).
        services.AddDbContext<SimulationDbContext>(options =>
            options.UseSqlite(connectionString));

        // Registra o executor como Singleton
        services.AddSingleton<IScopedExecutor, ScopedExecutor>();
        
        // Registra a fila de tarefas como Singleton
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        // Registra o serviço que vai consumir a fila (o QueuedHostedService que vimos antes)
        services.AddHostedService<QueuedHostedService>();
        
        // Em ServicesPersistenceExtensions.cs ou Program.cs
        services.AddSingleton<IPlayerStagingArea, PlayerStagingArea>();

        services.AddScoped(typeof(IRepository<,>), typeof(InMemoryRepository<,>));
        services.AddScoped(typeof(IRepositoryAsync<,>), typeof(EFCoreRepository<,>));

        return services;
    }
}