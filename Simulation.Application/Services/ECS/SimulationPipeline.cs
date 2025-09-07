using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;

namespace Simulation.Application.Services.ECS;

/// <summary>
/// Define a ordem de execução de todos os sistemas da simulação a cada tick.
/// A ordem é crítica para garantir um fluxo de dados previsível e estável.
/// </summary>
public sealed class SimulationPipeline : BaseSystem<World, float>
{
    private readonly ISystem<float>[] _systems;
    private readonly string[] _systemNames;
    private readonly ILogger<SimulationPipeline> _logger;

    // Precompiled logging delegates to avoid allocations on the hot path.
    private static readonly Action<ILogger, string, Exception?> LogBeforeUpdateError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(LogBeforeUpdateError)),
            "Exception in BeforeUpdate of system {System}");

    private static readonly Action<ILogger, string, Exception?> LogUpdateError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, nameof(LogUpdateError)),
            "Exception in Update of system {System}");

    private static readonly Action<ILogger, string, Exception?> LogAfterUpdateError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, nameof(LogAfterUpdateError)),
            "Exception in AfterUpdate of system {System}");

    public SimulationPipeline(World world, IEnumerable<ISystem<float>> systems, ILogger<SimulationPipeline> logger) : base(world)
    {
        if (systems == null) throw new ArgumentNullException(nameof(systems));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Materializa em array apenas uma vez no construtor — fast access sem alocações por tick.
        _systems = systems as ISystem<float>[] ?? systems.ToArray();
        _systemNames = new string[_systems.Length];
        for (int i = 0; i < _systems.Length; i++)
        {
            // Cache do nome do sistema para evitar chamadas repetidas a GetType().Name no hot-path.
            _systemNames[i] = _systems[i]?.GetType().Name ?? "<null-system>";
        }
    }
        
    public override void Initialize()
    {
        foreach (var system in _systems)
        {
            system.Initialize();
        }
    }

    public override void BeforeUpdate(in float t)
    {
        // snapshot local para evitar vários acessos ao campo.
        var systems = _systems;
        var names = _systemNames;
        var logger = _logger;
        var n = systems.Length;
        if (n == 0) return;
            
        // --- Phase 1: BeforeUpdate ---
        for (int i = 0; i < n; i++)
        {
            try
            {
                systems[i].BeforeUpdate(in t);
            }
            catch (Exception ex)
            {
                LogBeforeUpdateError(logger, names[i], ex);
                // continuar — não deixamos uma falha em um sistema cancelar todo o tick
            }
        }
    }

    public override void Update(in float t)
    {
        // snapshot local para evitar vários acessos ao campo.
        var systems = _systems;
        var names = _systemNames;
        var logger = _logger;

        var n = systems.Length;
        if (n == 0) return;
        // --- Phase 2: Update ---
        for (int i = 0; i < n; i++)
        {
            try
            {
                systems[i].Update(in t);
            }
            catch (Exception ex)
            {
                LogUpdateError(logger, names[i], ex);
            }
        }
    }

    public override void AfterUpdate(in float t)
    {
        // snapshot local para evitar vários acessos ao campo.
        var systems = _systems;
        var names = _systemNames;
        var logger = _logger;

        var n = systems.Length;
        if (n == 0) return;
        // --- Phase 3: AfterUpdate ---
        for (int i = 0; i < n; i++)
        {
            try
            {
                systems[i].AfterUpdate(in t);
            }
            catch (Exception ex)
            {
                LogAfterUpdateError(logger, names[i], ex);
            }
        }
    }
}