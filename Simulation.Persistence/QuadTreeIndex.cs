using System.Drawing;
using Arch.Core;
using Microsoft.Extensions.Options;
using QuadTrees;
using QuadTrees.QTreeRect;
using Simulation.Application.Options;
using Simulation.Application.Ports.Index;
using Simulation.Domain.Components;

namespace Simulation.Persistence;

/// <summary>
/// Adapter que implementa o ISpatialIndex usando a biblioteca Splitice/QuadTrees.
/// </summary>
public class QuadTreeIndex : ISpatialIndex
{
    private class QuadTreeItem(Entity entity, Position pos) : IRectQuadStorable
    {
        public Entity Entity { get; } = entity;
        public Rectangle Rect { get; set; } = new(pos.X, pos.Y, 1, 1); // Assumindo tamanho 1x1
    }
    
    private readonly QuadTreeRect<QuadTreeItem> _qtree;
    private readonly Dictionary<Entity, QuadTreeItem> _items = new();

    public QuadTreeIndex(int minX, int minY, int width, int height)
    {
        _qtree = new QuadTreeRect<QuadTreeItem>(new Rectangle(minX, minY, width, height));
    }

    public void Add(Entity entity, Position position)
    {
        if (_items.ContainsKey(entity)) return;
        var item = new QuadTreeItem(entity, position);
        _items[entity] = item;
        _qtree.Add(item);
    }

    public void Remove(Entity entity)
    {
        if (_items.Remove(entity, out var item))
        {
            _qtree.Remove(item);
        }
    }

    public void Update(Entity entity, Position newPosition)
    {
        if (_items.TryGetValue(entity, out var item))
        {
            // A forma mais segura de atualizar é remover e adicionar novamente
            _qtree.Remove(item);
            item.Rect = new Rectangle(newPosition.X, newPosition.Y, 1, 1);
            _qtree.Add(item);
        }
    }
    
    public void Query(Position center, int radius, List<Entity> results)
    {
        var searchRect = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        _qtree.GetObjects(searchRect, (obj) => results.Add(obj.Entity));
    }

    public List<Entity> Query(Position center, int radius)
    {
        var searchRect = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        var results = new List<QuadTreeItem>();
        _qtree.GetObjects(searchRect, results);

        var entities = new List<Entity>(results.Count);
        foreach (var item in results)
            entities.Add(item.Entity);
        return entities;
    }
}