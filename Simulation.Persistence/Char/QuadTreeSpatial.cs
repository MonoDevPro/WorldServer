using System.Drawing;
using Arch.Core;
using Simulation.Application.Ports.Map;
using Simulation.Domain.Components;

namespace Simulation.Persistence.Char;

/// <summary>
/// Simple spatial index implementation using a dictionary-based approach.
/// This is a temporary replacement for the QuadTree implementation.
/// </summary>
public class QuadTreeSpatial : ISpatialIndex
{
    private class SpatialItem
    {
        public Entity Entity { get; }
        public Position Position { get; set; }

        public SpatialItem(Entity entity, Position position)
        {
            Entity = entity;
            Position = position;
        }
    }
    
    private readonly Dictionary<Entity, SpatialItem> _items = new();
    private readonly int _width;
    private readonly int _height;

    public QuadTreeSpatial(int minX, int minY, int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Add(Entity entity, Position position)
    {
        if (_items.ContainsKey(entity)) return;
        var item = new SpatialItem(entity, position);
        _items[entity] = item;
    }

    public void Remove(Entity entity)
    {
        _items.Remove(entity);
    }

    public void Update(Entity entity, Position newPosition)
    {
        if (_items.TryGetValue(entity, out var item))
        {
            item.Position = newPosition;
        }
    }
    
    public void Query(Position center, int radius, List<Entity> results)
    {
        results.Clear();
        var radiusSquared = radius * radius;
        
        foreach (var item in _items.Values)
        {
            var dx = item.Position.X - center.X;
            var dy = item.Position.Y - center.Y;
            var distanceSquared = dx * dx + dy * dy;
            
            if (distanceSquared <= radiusSquared)
            {
                results.Add(item.Entity);
            }
        }
    }

    public List<Entity> Query(Position center, int radius)
    {
        var results = new List<Entity>();
        Query(center, radius, results);
        return results;
    }
}