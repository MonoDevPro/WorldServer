using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Client.Core;
using Simulation.Client.Network;
using Simulation.Client.Systems;

namespace Simulation.Client;

class Program
{
    static async Task Main(string[] args)
    {
        // --- 1. Configuração ---
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSimulationClient(configuration);
        await using var provider = services.BuildServiceProvider();

        // --- 2. Resolução dos Serviços ---
        var logger = provider.GetRequiredService<ILogger<Program>>();
        var networkClient = provider.GetRequiredService<LiteNetClient>();
        var snapshotHandler = provider.GetRequiredService<SnapshotHandlerSystem>();
        var inputManager = provider.GetRequiredService<InputManager>();
        
        logger.LogInformation("=== Cliente WorldServer v2.0 ===");
        
        // --- 3. Conexão ---
        networkClient.Connect();
        
        // --- 4. Loop Principal (Game Loop) ---
        var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, _) => cancellationTokenSource.Cancel();
        
        logger.LogInformation("Loop principal iniciado. Pressione Ctrl+C para sair.");

        try
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                // Etapa 1: Processa a Rede (recebe snapshots)
                networkClient.PollEvents();
                
                // Etapa 2: Atualiza o ECS (aplica os snapshots recebidos)
                snapshotHandler.Update(0.016f); // Passa um delta time fixo

                // Etapa 3: Processa o Input do Utilizador (envia intents)
                inputManager.ProcessInput(cancellationTokenSource);
                
                // Etapa 4: Espera para não sobrecarregar a CPU
                await Task.Delay(15, cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Esperado ao sair
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Erro fatal no loop principal.");
        }
        finally
        {
            logger.LogInformation("A finalizar o cliente...");
            networkClient.Dispose();
        }
    }
}