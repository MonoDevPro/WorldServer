using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;

namespace Simulation.Application.Services.ECS
{
    /// <summary>
    /// Define a ordem de execução de todos os sistemas da simulação a cada tick.
    /// A ordem é crítica para garantir um fluxo de dados previsível e estável.
    /// </summary>
    public sealed class SimulationPipeline
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

        public SimulationPipeline(IEnumerable<ISystem<float>> systems, ILogger<SimulationPipeline> logger)
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

        /// <summary>
        /// Executa um tick em três fases: BeforeUpdate, Update, AfterUpdate.
        /// Esta versão é otimizada para o hot path: evita allocations desnecessárias e usa delegates de log.
        /// </summary>
        public void Tick(World world, in float delta, CancellationToken ct = default)
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
                if (ct.IsCancellationRequested) return; // cheap check, evita throw/stackcost
                try
                {
                    systems[i].BeforeUpdate(in delta);
                }
                catch (Exception ex)
                {
                    LogBeforeUpdateError(logger, names[i], ex);
                    // continuar — não deixamos uma falha em um sistema cancelar todo o tick
                }
            }

            // --- Phase 2: Update ---
            for (int i = 0; i < n; i++)
            {
                if (ct.IsCancellationRequested) return;
                try
                {
                    systems[i].Update(in delta);
                }
                catch (Exception ex)
                {
                    LogUpdateError(logger, names[i], ex);
                }
            }

            // --- Phase 3: AfterUpdate ---
            for (int i = 0; i < n; i++)
            {
                if (ct.IsCancellationRequested) return;
                try
                {
                    systems[i].AfterUpdate(in delta);
                }
                catch (Exception ex)
                {
                    LogAfterUpdateError(logger, names[i], ex);
                }
            }
        }

        /// <summary>
        /// Variante .NET 7+ (micro-opt): iterar via span do List internal array
        /// usar somente se você mediu e precisa do último bit de performance.
        /// </summary>
        public void Tick_UsingAsSpan(World world, float delta, CancellationToken ct = default)
        {
#if NET7_0_OR_GREATER
            // Se você mantiver uma List<ISystem<float>> _list, poderia fazer:
            // var span = CollectionsMarshal.AsSpan(_list);
            // for (int i = 0; i < span.Length; i++) span[i].Update(world, delta);
#endif
            Tick(world, delta, ct); // fallback
        }

        /// <summary>
        /// Exemplo de paralelização por fase — usar apenas se os sistemas forem thread-safe e independentes.
        /// </summary>
        public void TickParallel(World world, float delta, CancellationToken ct = default)
        {
            var systems = _systems;
            var names = _systemNames;
            var logger = _logger;

            var options = new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = Environment.ProcessorCount };

            // BeforeUpdate em paralelo (apenas se for seguro)
            Parallel.For(0, systems.Length, options, i =>
            {
                try { systems[i].BeforeUpdate(in delta); }
                catch (Exception ex) { LogBeforeUpdateError(logger, names[i], ex); }
            });

            // Update em paralelo
            Parallel.For(0, systems.Length, options, i =>
            {
                try { systems[i].Update(in delta); }
                catch (Exception ex) { LogUpdateError(logger, names[i], ex); }
            });

            // AfterUpdate em paralelo
            Parallel.For(0, systems.Length, options, i =>
            {
                try { systems[i].AfterUpdate(in delta); }
                catch (Exception ex) { LogAfterUpdateError(logger, names[i], ex); }
            });
        }
    }
}
