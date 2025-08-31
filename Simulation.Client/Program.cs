using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Client;
using Simulation.Client.Core;

namespace Simulation.Client;

class Program
{
    static async Task Main(string[] args)
    {
        // --- Construção da Configuração ---
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // --- Construção do Container de Injeção de Dependência ---
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Adiciona todos os serviços do cliente
        services.AddSimulationClient(configuration);

        // Constrói o provedor de serviços
        await using var provider = services.BuildServiceProvider();

        // --- Resolução dos Serviços Principais ---
        var clientLoop = provider.GetRequiredService<ClientLoop>();
        var logger = provider.GetRequiredService<ILogger<Program>>();

        // --- Configuração do Encerramento Graceful (Ctrl+C) ---
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            logger.LogInformation("Ctrl+C recebido. Sinalizando para o encerramento...");
            cts.Cancel();
        };

        // --- Execução do Ciclo de Vida do Cliente ---
        try
        {
            logger.LogInformation("=== Cliente WorldServer ===");
            logger.LogInformation("Iniciando cliente...");
            
            // Etapa 1: Inicializa o cliente (conecta ao servidor)
            await clientLoop.InitializeAsync(cts.Token).ConfigureAwait(false);
            
            // Etapa 2: Executa o loop principal do cliente
            await clientLoop.RunAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Operação cancelada. O cliente será encerrado.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Erro fatal não tratado que encerrou o cliente.");
        }
        finally
        {
            // Etapa 3: Garante o descarte de recursos de forma limpa
            logger.LogInformation("Iniciando o processo de finalização...");
            await clientLoop.DisposeAsync().ConfigureAwait(false);
            logger.LogInformation("Cliente finalizado.");
        }
    }
}