using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Persistence;

namespace Simulation.Persistence;

public class QueuedHostedService(IServiceProvider serviceProvider, IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Aguarda por uma nova tarefa na fila
            var workItem = await taskQueue.DequeueAsync(stoppingToken);

            // O escopo é criado AQUI, de forma padronizada para cada item da fila.
            using var scope = serviceProvider.CreateScope();
            try
            {
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro executando tarefa em segundo plano.");
            }
        }
    }
}