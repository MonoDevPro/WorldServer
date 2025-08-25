using Arch.Core;
using Simulation.Core.Abstractions.Commons.Components.Map;

namespace Simulation.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly World _world;

    public Worker(ILogger<Worker> logger, World world)
    {
        _logger = logger;
        _world = world;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Cria uma entidade de comando para carregar o mapa 1 ao iniciar.
        _world.Create(new WantsToLoadMap { MapId = 1 });
        _logger.LogInformation("Comando para carregar mapa 1 enfileirado.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}