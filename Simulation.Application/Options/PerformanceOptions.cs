namespace Simulation.Application.Options;

public sealed class PerformanceOptions
{
    public const string SectionName = "Performance";
    public bool EnablePipelineInstrumentation { get; set; } = true;
    public bool SkipFirstPerformanceReport { get; set; } = true;
    public double PerformanceReportIntervalSeconds { get; set; } = 30;
}
