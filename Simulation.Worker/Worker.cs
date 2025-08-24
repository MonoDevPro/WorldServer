using Arch.Core;

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
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}