using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Console;
using Simulation.Core;
using Simulation.Core.Abstractions.Adapters.Map;
using Simulation.Core.Abstractions.Ports.Map;
using Simulation.Network;

// 1. Construção da Configuração
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// 2. Construção do Container de Injeção de Dependência
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Registra as opções de configuração
services.Configure<NetworkOptions>(configuration.GetSection(NetworkOptions.SectionName));
services.Configure<WorldOptions>(configuration.GetSection(WorldOptions.SectionName));

// Adiciona todos os serviços dos projetos Core e Network
services.AddSimulationCore(configuration);
services.AddSimulationNetwork(configuration);

// Registra os serviços específicos da aplicação Console
services.AddSingleton<IMapLoaderService, MapLoaderService>();
services.AddSingleton<ServerLoop>();

// Constrói o provedor de serviços
await using var provider = services.BuildServiceProvider();

// 3. Resolução dos Serviços Principais
var loop = provider.GetRequiredService<ServerLoop>();
var logger = provider.GetRequiredService<ILogger<Program>>();

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
    // Etapa 1: Inicializa o servidor (carrega mapas, inicia a rede, etc.)
    await loop.InitializeAsync(cts.Token).ConfigureAwait(false);
    
    // Etapa 2: Executa o loop principal do jogo
    await loop.RunAsync(cts.Token).ConfigureAwait(false);
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
    await loop.DisposeAsync().ConfigureAwait(false);
    logger.LogInformation("Servidor finalizado.");
}

