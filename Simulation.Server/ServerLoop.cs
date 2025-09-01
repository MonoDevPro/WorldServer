using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Map;
using Simulation.Application.Services;
using Simulation.Networking;

namespace Simulation.Server;

/// <summary>
/// Orquestra o ciclo de vida do servidor, incluindo inicialização, o loop principal do jogo e o desligamento.
/// </summary>
public class ServerLoop : IAsyncDisposable
{
    private const double TickSeconds = 1.0 / 60.0; // 60 ticks por segundo
    private readonly Stopwatch _mainTimer = new();
    
    private readonly ILogger<ServerLoop> _logger;
    private readonly SimulationRunner _simulationRunner;
    private readonly LiteNetServer _networkServer;
    private readonly IMapLoaderService _mapLoaderService;

    public ServerLoop(
        ILogger<ServerLoop> logger,
        SimulationRunner simulationRunner,
        IMapLoaderService mapLoaderService,
        LiteNetServer networkServer)
    {
        _logger = logger;
        _simulationRunner = simulationRunner;
        _mapLoaderService = mapLoaderService;
        _networkServer = networkServer;
    }

    /// <summary>
    /// Prepara o servidor para execução, carregando recursos essenciais.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Inicializando o servidor...");
        try
        {
            // Carrega o mapa inicial (ex: mapa 1) antes de aceitar conexões.
            // O MapLoaderService enfileira o trabalho, que será processado no primeiro tick pelo MapLoaderSystem.
            await _mapLoaderService.LoadMapAsync(0, cancellationToken);
            await _mapLoaderService.LoadMapAsync(1, cancellationToken);
            _logger.LogInformation("Mapa inicial (ID 1) enfileirado para carregamento.");
            
            // Inicia a camada de rede
            _networkServer.Start();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Falha crítica durante a inicialização do servidor.");
            throw;
        }
    }

    /// <summary>
    /// Executa o loop principal da simulação.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Entrando no loop principal da simulação...");
        _mainTimer.Start();
        
        double accumulator = 0;
        double lastTime = _mainTimer.Elapsed.TotalSeconds;

        while (!cancellationToken.IsCancellationRequested)
        {
            var currentTime = _mainTimer.Elapsed.TotalSeconds;
            var deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            // Limita o deltaTime para evitar a "espiral da morte" se houver um pico de lag.
            if (deltaTime > 0.25)
            {
                deltaTime = 0.25;
            }

            accumulator += deltaTime;

            // --- Etapa 1: Processamento de Rede ---
            _networkServer.PollEvents(); // Processa todos os pacotes de rede recebidos.

            // --- Etapa 2: Simulação com Timestep Fixo ---
            while (accumulator >= TickSeconds)
            {
                try
                {
                    _simulationRunner.Update((float)TickSeconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante o update do SimulationRunner.");
                }
                accumulator -= TickSeconds;
            }

            // --- Etapa 3: Estratégia de Espera (Sleep) ---
            await SleepUntilNextTick(accumulator, cancellationToken);
        }
    }

    private async Task SleepUntilNextTick(double accumulator, CancellationToken cancellationToken)
    {
        var remainingTime = TickSeconds - accumulator;
        if (remainingTime <= 0)
        {
            await Task.Yield(); // Estamos atrasados, cede o controle do thread brevemente.
            return;
        }

        var delayMilliseconds = (int)(remainingTime * 1000);
        if (delayMilliseconds > 1)
        {
            try
            {
                await Task.Delay(delayMilliseconds - 1, cancellationToken);
            }
            catch (TaskCanceledException) { /* Ignora, o cancelamento será tratado no loop principal. */ }
        }
        else
        {
            await Task.Yield(); // Para esperas muito curtas, Task.Yield é mais apropriado.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Finalizando o servidor...");
        _networkServer.Stop();
        
        // Exemplo de como descartar outros recursos, se necessário
        if (_simulationRunner is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
    }
}
