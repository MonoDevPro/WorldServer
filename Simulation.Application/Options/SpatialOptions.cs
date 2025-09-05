namespace Simulation.Application.Options;
public class SpatialOptions
{
    public const string SectionName = "Spatial";
    
    public int MinX { get; set; } = -1024;
    public int MinY { get; set; } = -1024;
    public int Width { get; set; } = 2048;
    private int _height = 2048;
    public int Height { get { return _height; } set { _height = value; } }
    // Area of Interest radius in tiles for broadcast filtering
    public int InterestRadius { get; set; } = 15;
}
