using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Application.Systems;
using Simulation.Application.Systems.In;
using Simulation.Application.Systems.Out;
using CharSnapshotPublisherSystem = Simulation.Application.Systems.Out.CharSnapshotPublisherSystem;

namespace Simulation.Application.Services;

/// <summary>
/// Define a ordem de execução de todos os sistemas da simulação a cada tick.
/// A ordem é crítica para garantir um fluxo de dados previsível e estável.
/// </summary>
public class SimulationPipeline(IEnumerable<ISystem<float>> systems, ILogger<SimulationPipeline> logger)
{
    private readonly ISystem<float>[] _systems = systems.ToArray();

    /// <summary>
    /// Executa um tick em três fases: BeforeUpdate, Update, AfterUpdate.
    /// </summary>
    public void Tick(World world, in float delta, CancellationToken ct = default)
    {
        // snapshot local para otimizar acesso
        var systems = _systems;
        var n = systems.Length;

        // --- Phase 1: BeforeUpdate ---
        for (int i = 0; i < n; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                systems[i].BeforeUpdate(in delta);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception in BeforeUpdate of system {System}", systems[i].GetType().Name);
                // continuar — não deixamos uma falha em um sistema cancelar todo o tick
            }
        }

        // --- Phase 2: Update ---
        for (int i = 0; i < n; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                systems[i].Update(in delta);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception in Update of system {System}", systems[i].GetType().Name);
            }
        }

        // --- Phase 3: AfterUpdate ---
        for (int i = 0; i < n; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                systems[i].AfterUpdate(in delta);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception in AfterUpdate of system {System}", systems[i].GetType().Name);
            }
        }
    }
    
    // Variante .NET 7+ (micro-opt): iterar via span do List internal array
    // usar somente se você mediu e precisa do último bit de performance.
    public void Tick_UsingAsSpan(World world, float delta)
    {
#if NET7_0_OR_GREATER
        // collectionsMarshal requires you to hold a List; we already have array so not necessary.
        // If you kept a List<BaseSystem<World,float>> _list; you could do:
        // var span = CollectionsMarshal.AsSpan(_list);
        // for (int i = 0; i < span.Length; i++) span[i].Update(world, delta);
#endif
        Tick(world, delta); // fallback
    }
    
    // Exemplo de paralelização por fase (pseudocódigo)
    public void TickParallelAsync(World world, float delta, CancellationToken ct = default)
    {
        var systems = _systems;
        var options = new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = Environment.ProcessorCount };

        // BeforeUpdate em paralelo (apenas se for seguro)
        Parallel.For(0, systems.Length, options, i =>
        {
            try { systems[i].BeforeUpdate(in delta); }
            catch (Exception ex) { logger.LogError(ex, "BeforeUpdate failed in {System}", systems[i].GetType().Name); }
        });

        // Update em paralelo
        Parallel.For(0, systems.Length, options, i =>
        {
            try { systems[i].Update(in delta); }
            catch (Exception ex) { logger.LogError(ex, "Update failed in {System}", systems[i].GetType().Name); }
        });

        // AfterUpdate em paralelo
        Parallel.For(0, systems.Length, options, i =>
        {
            try { systems[i].AfterUpdate(in delta); }
            catch (Exception ex) { logger.LogError(ex, "AfterUpdate failed in {System}", systems[i].GetType().Name); }
        });
    }
}