namespace Simulation.Application.Options;
public class WorldOptions
{
    public const string SectionName = "World";

    // World
    public int ChunkSizeInBytes { get; set; } = 16_384;
    public int MinimumAmountOfEntitiesPerChunk { get; set; } = 100;
    public int ArchetypeCapacity { get; set; } = 2;
    public int EntityCapacity { get; set; } = 64;
    
    // Spatial
    public int MinX { get; set; } = -1024;
    public int MinY { get; set; } = -1024;
    public int Width { get; set; } = 2048;
    public int Height { get; set; } = 2048;
}