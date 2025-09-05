using System.Collections.Concurrent;
using System.Drawing;
using Arch.Core;
using QuadTrees;
using QuadTrees.QTreeRect;
using Simulation.Application.Ports.ECS.Utils.Indexers;
using Simulation.Domain.Components;

namespace Simulation.Persistence.Char;

/// <summary>
/// Adapter que implementa o ISpatialIndex usando a biblioteca Splitice/QuadTrees.
/// Inclui object pooling para reduzir alocações em queries de alta frequência.
/// </summary>
public class QuadTreeSpatial : ISpatialIndex
{
    private class QuadTreeItem(Entity entity, Position pos) : IRectQuadStorable
    {
        public Entity Entity { get; } = entity;
        public Rectangle Rect { get; set; } = new(pos.X, pos.Y, 1, 1); // Assumindo tamanho 1x1
    }
    
    private readonly QuadTreeRect<QuadTreeItem> _qtree;
    private readonly Dictionary<Entity, QuadTreeItem> _items = new();
    
    // Object pool para reduzir alocações em queries
    private static readonly ConcurrentQueue<List<Entity>> _entityListPool = new();
    private static readonly ConcurrentQueue<List<QuadTreeItem>> _itemListPool = new();

    public QuadTreeSpatial(int minX, int minY, int width, int height)
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
        
        // Usa object pooling para lista intermediária
        var itemResults = GetPooledItemList();
        try
        {
            _qtree.GetObjects(searchRect, itemResults);
            
            results.Clear();
            foreach (var item in itemResults)
                results.Add(item.Entity);
        }
        finally
        {
            ReturnPooledItemList(itemResults);
        }
    }

    public List<Entity> Query(Position center, int radius)
    {
        var searchRect = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        
        // Usa object pooling para ambas as listas
        var itemResults = GetPooledItemList();
        var entityResults = GetPooledEntityList();
        
        try
        {
            _qtree.GetObjects(searchRect, itemResults);
            
            foreach (var item in itemResults)
                entityResults.Add(item.Entity);
                
            // Retorna uma nova lista para evitar problemas de ownership
            return new List<Entity>(entityResults);
        }
        finally
        {
            ReturnPooledItemList(itemResults);
            ReturnPooledEntityList(entityResults);
        }
    }
    
    private static List<Entity> GetPooledEntityList()
    {
        if (_entityListPool.TryDequeue(out var list))
        {
            list.Clear();
            return list;
        }
        return new List<Entity>();
    }
    
    private static void ReturnPooledEntityList(List<Entity> list)
    {
        if (list != null && _entityListPool.Count < 20) // Limita o pool
        {
            list.Clear();
            _entityListPool.Enqueue(list);
        }
    }
    
    private static List<QuadTreeItem> GetPooledItemList()
    {
        if (_itemListPool.TryDequeue(out var list))
        {
            list.Clear();
            return list;
        }
        return new List<QuadTreeItem>();
    }
    
    private static void ReturnPooledItemList(List<QuadTreeItem> list)
    {
        if (list != null && _itemListPool.Count < 20) // Limita o pool
        {
            list.Clear();
            _itemListPool.Enqueue(list);
        }
    }
}