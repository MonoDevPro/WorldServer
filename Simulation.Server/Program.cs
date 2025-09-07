using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Application.Services.Loop;
using Simulation.Server;

// 2. Construção do Container de Injeção de Dependência
var services = new ServiceCollection();
// Adiciona os serviços do servidor
services.AddServerServices();

// Constrói o provedor de serviços
await using var provider = services.BuildServiceProvider();

// 3. Resolução dos Serviços Principais

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