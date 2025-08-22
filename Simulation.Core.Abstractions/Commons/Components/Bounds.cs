namespace Simulation.Core.Components;

public struct Bounds
{
    public int MinX;
    public int MinY;
    public int MaxX;
    public int MaxY;
    
    public bool Contains(TilePosition p) => p.Position.X >= MinX && p.Position.X <= MaxX && p.Position.Y >= MinY && p.Position.Y <= MaxY;
}
