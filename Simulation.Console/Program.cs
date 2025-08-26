using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Simulation.Console;
using Simulation.Core;
using Simulation.Network;

// --- Início da nova seção de configuração ---
// 1) Construção da configuração
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory) // Garante que ele encontre o JSON
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
// --- Fim da nova seção de configuração ---

// 2) Construção do container
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());

// --- Registra as opções de configuração ---
services.Configure<NetworkOptions>(configuration.GetSection(NetworkOptions.SectionName));

services.AddSimulationCore();
services.AddSimulationNetwork();
services.AddSingleton<SimulationLoop>();
services.Replace(ServiceDescriptor.Singleton<SimulationPipeline>(sp => new SystemPipelineAdapter(sp)));

// Alternativa (recomendada caso o adapter tenha dependências resolvíveis):
// services.AddSingleton<SystemPipelineAdapter>();
// services.AddSingleton<SimulationPipeline>(sp => sp.GetRequiredService<SystemPipelineAdapter>());

await using var provider = services.BuildServiceProvider();

// 2) resoluções
var loop = provider.GetRequiredService<SimulationLoop>();
var logger = provider.GetRequiredService<ILogger<Program>>(); // ou ILogger<SimulationLoop>

// 3) CancellationTokenSource centralizado e handler registrado ANTES de iniciar o loop
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    // sinaliza que queremos terminar e evita que o processo finalize imediatamente
    eventArgs.Cancel = true;
    logger.LogInformation("Ctrl+C recebido — sinalizando cancelamento...");
    cts.Cancel();
};

// 4) Inicia o loop; StartAsync observa o token e retorna quando o loop terminar (ou token cancelado)
try
{
    await loop.StartAsync(cts.Token).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Loop cancelado.");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Erro fatal durante a execução do loop.");
}
finally
{
    // 5) Cleanup e dispose ordenados (DisposeAsync do loop e do provider)
    try
    {
        await loop.DisposeAsync().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Erro ao finalizar SimulationLoop.");
    }

    // provider será disposed automaticamente pelo await using acima
    logger.LogInformation("Servidor finalizado.");
}
