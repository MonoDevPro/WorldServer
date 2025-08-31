using Arch.Core;
using Microsoft.Extensions.Logging;
using Simulation.Client.Network;
using Simulation.Client.Systems;
using Simulation.Core.Abstractions.Adapters;

namespace Simulation.Client.Core;

/// <summary>
/// Loop principal do cliente que coordena a comunicação de rede, atualização do ECS e entrada do usuário.
/// Segue o mesmo padrão do SimulationLoop do servidor.
/// </summary>
public class ClientLoop : IAsyncDisposable
{
    private readonly ILogger<ClientLoop> _logger;
    private readonly LiteNetClient _networkClient;
    private readonly SnapshotHandlerSystem _snapshotHandlerSystem;
    private readonly InputManager _inputManager;
    private readonly World _world;
    
    private const double TickSeconds = 1.0 / 60.0; // 60 ticks por segundo
    private readonly System.Diagnostics.Stopwatch _mainTimer = new();

    public ClientLoop(
        ILogger<ClientLoop> logger,
        LiteNetClient networkClient,
        SnapshotHandlerSystem snapshotHandlerSystem,
        InputManager inputManager,
        World world)
    {
        _logger = logger;
        _networkClient = networkClient;
        _snapshotHandlerSystem = snapshotHandlerSystem;
        _inputManager = inputManager;
        _world = world;
    }

    /// <summary>
    /// Inicializa o cliente e conecta ao servidor
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Inicializando cliente...");
        
        try
        {
            // Conecta ao servidor
            _networkClient.Connect();
            
            // Aguarda um momento para a conexão estabelecer (com retry)
            var maxRetries = 10;
            var retryDelay = 500; // ms
            
            for (int i = 0; i < maxRetries; i++)
            {
                await Task.Delay(retryDelay, cancellationToken);
                _networkClient.PollEvents();
                
                if (_networkClient.IsConnected)
                {
                    _logger.LogInformation("Cliente conectado ao servidor com sucesso");
                    return;
                }
            }
            
            _logger.LogWarning("Cliente não conseguiu conectar ao servidor após {Retries} tentativas", maxRetries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante inicialização do cliente");
            throw;
        }
    }

    /// <summary>
    /// Executa o loop principal do cliente
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando loop principal do cliente...");
        _mainTimer.Start();

        // Inicia o loop de entrada do usuário em uma task separada
        var inputTask = _inputManager.StartInputLoopAsync(cancellationToken);

        double accumulator = 0;
        double lastTime = _mainTimer.Elapsed.TotalSeconds;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var currentTime = _mainTimer.Elapsed.TotalSeconds;
                var deltaTime = currentTime - lastTime;
                lastTime = currentTime;

                // Limita o deltaTime para evitar problemas com picos de lag
                if (deltaTime > 0.25)
                {
                    deltaTime = 0.25;
                }

                accumulator += deltaTime;

                // --- Etapa 1: Processamento de Rede ---
                _networkClient.PollEvents();

                // --- Etapa 2: Atualização do ECS ---
                while (accumulator >= TickSeconds)
                {
                    // Atualiza os sistemas do ECS
                    _snapshotHandlerSystem.Update((float)TickSeconds);
                    
                    accumulator -= TickSeconds;
                }

                // Pequena pausa para não sobrecarregar a CPU
                await Task.Delay(1, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Loop principal cancelado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no loop principal do cliente");
            throw;
        }
        finally
        {
            // Aguarda o input task terminar
            try
            {
                await inputTask;
            }
            catch (OperationCanceledException)
            {
                // Esperado quando cancelamos
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Finalizando cliente...");
        
        try
        {
            _networkClient.Disconnect();
            _networkClient.Dispose();
            _inputManager.Dispose();
            _world.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante finalização do cliente");
        }
        
        _logger.LogInformation("Cliente finalizado");
    }
}