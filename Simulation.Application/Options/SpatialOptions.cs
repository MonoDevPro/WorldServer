namespace Simulation.Application.Options;
public class SpatialOptions
{
    public const string SectionName = "Spatial";
    
    public int MinX { get; set; } = -1024;
    public int MinY { get; set; } = -1024;
    public int Width { get; set; } = 2048;
    public int Height { get; set; } = 2048;
}
