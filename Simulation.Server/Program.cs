using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Persistence;
using Simulation.Application.Services.Loop;
using Simulation.Persistence;
using Simulation.Persistence.Configurations;
using Simulation.Server;

var services = new ServiceCollection();
services.AddServerServices();

await using var provider = services.BuildServiceProvider();

var executorQueue = provider.GetRequiredService<IBackgroundTaskQueue>();
executorQueue.QueueBackgroundWorkItem(async (sp, ct) =>
{
    var dbContext = sp.GetRequiredService<SimulationDbContext>();
    await DataSeeder.SeedDatabaseAsync(dbContext);
    var count = await dbContext.PlayerTemplates.CountAsync(ct) + await dbContext.MapTemplates.CountAsync(ct);
    
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Número de entidades no banco de dados: {Count}", count);
});


var logger = provider.GetRequiredService<ILogger<Program>>();
var gameloop = provider.GetRequiredService<GameLoop>();

// 4. Configuração do Encerramento Graceful (Ctrl+C)
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    logger.LogInformation("Ctrl+C recebido. Sinalizando para o encerramento...");
    cts.Cancel();
};

// 5. Execução do Ciclo de Vida do Servidor
try
{
    // Etapa 2: Executa o loop principal do jogo
    logger.LogInformation("Iniciando o servidor...");
    await gameloop.RunAsync(cts.Token).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Operação cancelada. O servidor será encerrado.");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Erro fatal não tratado que encerrou a aplicação.");
}
finally
{
    // Etapa 3: Garante o descarte de recursos de forma limpa
    logger.LogInformation("Iniciando o processo de finalização...");
    await gameloop.DisposeAsync().ConfigureAwait(false);
    logger.LogInformation("Servidor finalizado.");
}