using System.Diagnostics;
using Arch.Core;
using Simulation.Core;
using Simulation.Core.Abstractions.Commons.Components.Map;
using Simulation.Network;
using Microsoft.Extensions.Logging;

namespace Simulation.Worker;

public class Worker(ILogger<Worker> logger, SimulationRunner runner, NetworkSystem network, World world)
    : BackgroundService
{
    // 60 ticks por segundo (16.666...ms)
    private const double TickSeconds = 1.0 / 60.0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Simulation started");
        var sw = Stopwatch.StartNew();
        double accumulator = 0;
        var last = sw.Elapsed.TotalSeconds;

        // Network já deve ter sido iniciado em StartAsync; porém toleramos se não.
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = sw.Elapsed.TotalSeconds;
                var frame = now - last;
                last = now;
                accumulator += frame;

                // 2) Fixed-step update: processa ticks completos
                while (accumulator >= TickSeconds)
                {
                    try
                    {
                        runner.Update((float)TickSeconds);
                    }
                    catch (Exception ex)
                    {
                        // Erros na simulação não devem travar o loop sem diagnóstico
                        logger.LogError(ex, "Erro no SimulationRunner.Update()");
                    }
                    accumulator -= TickSeconds;
                }

                // 3) Dorme um pouco para não ocupar 100% da CPU.
                var sleep = Math.Max(0.0, TickSeconds - accumulator);
                var delayMs = (int)(sleep * 1000.0 / 2.0); // meio tick de folga
                if (delayMs > 0)
                {
                    try
                    {
                        await Task.Delay(delayMs, stoppingToken).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException) { /* shutting down */ }
                }
                else
                {
                    // sem tempo de sleep: yield para manter responsividade
                    await Task.Yield();
                }
            }
        }
        finally
        {
            // Cleanup garantido: tenta parar a rede quando o loop terminar
            try
            {
                network.Stop();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Erro ao parar Network ao finalizar Worker");
            }
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Enfileira comando para carregar mapa 1
        world.Create(new WantsToLoadMap { MapId = 1 });
        logger.LogInformation("Comando para carregar mapa 1 enfileirado.");

        // Inicia o servidor de rede aqui (sincronamente) — se falhar, abortamos a inicialização.
        try
        {
            if (!network.Start())
            {
                logger.LogCritical("Falha ao iniciar network. Abortando startup do Worker.");
                // lançar exceção faz com que o Host perceba falha na inicialização do hosted service
                throw new InvalidOperationException("Falha ao iniciar network");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Exceção ao iniciar o network no StartAsync.");
            throw; // rethrow para o host lidar (gera shutdown)
        }

        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        // Garantir cleanup extra em Dispose (defensivo)
        try
        {
            network.Stop();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erro ao parar network no Dispose");
        }

        try
        {
            world.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erro ao Dispor o World no Dispose");
        }

        base.Dispose();
    }
}