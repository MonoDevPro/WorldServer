namespace Simulation.Core.Abstractions.Adapters.Map;

public class WorldOptions
{
    public const string SectionName = "World";
    
    public int MinX { get; set; } = -1024;
    public int MinY { get; set; } = -1024;
    public int Width { get; set; } = 2048;
    public int Height { get; set; } = 2048;
}
