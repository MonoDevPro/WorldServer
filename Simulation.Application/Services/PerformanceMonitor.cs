using Microsoft.Extensions.Logging;

namespace Simulation.Application.Services;

/// <summary>
/// Serviço de monitoramento de performance simples para detectar problemas críticos.
/// Monitora métricas essenciais como duração de ticks, GC pressure, e alocações.
/// </summary>
public class PerformanceMonitor : IDisposable
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly Timer _reportingTimer;
    private long _totalTickCount;
    private long _totalTickTimeMs;
    private long _slowTickCount;
    private long _lastGcGen0Count;
    private long _lastGcGen1Count;
    private long _lastGcGen2Count;
    private readonly object _statsLock = new();

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
        _lastGcGen0Count = GC.CollectionCount(0);
        _lastGcGen1Count = GC.CollectionCount(1);
        _lastGcGen2Count = GC.CollectionCount(2);
        
        // Relatório a cada 30 segundos
        _reportingTimer = new Timer(ReportStats, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Monitora a duração de um tick de simulação
    /// </summary>
    public void RecordTick(double tickDurationMs)
    {
        lock (_statsLock)
        {
            _totalTickCount++;
            _totalTickTimeMs += (long)tickDurationMs;
            
            // Considera tick lento se demorar mais que 20ms (assumindo 60 FPS = 16.67ms target)
            if (tickDurationMs > 20.0)
            {
                _slowTickCount++;
                
                // Log imediato para ticks muito lentos
                if (tickDurationMs > 50.0)
                {
                    _logger.LogWarning("Tick muito lento detectado: {TickDuration}ms (target: 16.67ms)", 
                        tickDurationMs.ToString("F2"));
                }
            }
        }
    }

    /// <summary>
    /// Relatório periódico de estatísticas de performance
    /// </summary>
    private void ReportStats(object? state)
    {
        try
        {
            lock (_statsLock)
            {
                if (_totalTickCount == 0) return;

                var avgTickTime = (double)_totalTickTimeMs / _totalTickCount;
                var slowTickPercentage = (_slowTickCount * 100.0) / _totalTickCount;

                // Métricas de GC
                var currentGen0 = GC.CollectionCount(0);
                var currentGen1 = GC.CollectionCount(1);
                var currentGen2 = GC.CollectionCount(2);

                var gen0Collections = currentGen0 - _lastGcGen0Count;
                var gen1Collections = currentGen1 - _lastGcGen1Count;
                var gen2Collections = currentGen2 - _lastGcGen2Count;

                _lastGcGen0Count = currentGen0;
                _lastGcGen1Count = currentGen1;
                _lastGcGen2Count = currentGen2;

                // Memória alocada
                var totalMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

                _logger.LogInformation(
                    "Performance Report - " +
                    "Avg Tick: {AvgTick}ms, " +
                    "Slow Ticks: {SlowTickPerc}%, " +
                    "Memory: {MemoryMB}MB, " +
                    "GC: Gen0={Gen0} Gen1={Gen1} Gen2={Gen2}",
                    avgTickTime.ToString("F2"),
                    slowTickPercentage.ToString("F1"),
                    totalMemoryMB.ToString("F1"),
                    gen0Collections,
                    gen1Collections,
                    gen2Collections);

                // Alerta se performance está ruim
                if (slowTickPercentage > 10.0)
                {
                    _logger.LogWarning("Alta percentagem de ticks lentos: {SlowTickPerc}% - Possível problema de performance", 
                        slowTickPercentage.ToString("F1"));
                }

                if (gen2Collections > 0)
                {
                    _logger.LogWarning("GC Gen2 detectado: {Gen2Collections} collections - Possível vazamento de memória ou alta pressão de GC", 
                        gen2Collections);
                }

                // Reset counters para próximo período
                _totalTickCount = 0;
                _totalTickTimeMs = 0;
                _slowTickCount = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de performance");
        }
    }

    /// <summary>
    /// Obtém estatísticas atuais (útil para debugging)
    /// </summary>
    public PerformanceStats GetCurrentStats()
    {
        lock (_statsLock)
        {
            return new PerformanceStats
            {
                TotalTicks = _totalTickCount,
                AverageTickTimeMs = _totalTickCount > 0 ? (double)_totalTickTimeMs / _totalTickCount : 0,
                SlowTickPercentage = _totalTickCount > 0 ? (_slowTickCount * 100.0) / _totalTickCount : 0,
                TotalMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
        }
    }

    public void Dispose()
    {
        _reportingTimer?.Dispose();
    }
}

public record struct PerformanceStats
{
    public long TotalTicks { get; init; }
    public double AverageTickTimeMs { get; init; }
    public double SlowTickPercentage { get; init; }
    public double TotalMemoryMB { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
}