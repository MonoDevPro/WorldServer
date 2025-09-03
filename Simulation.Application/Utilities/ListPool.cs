using System.Collections.Concurrent;
using Simulation.Domain.Templates;

namespace Simulation.Application.Utilities;

/// <summary>
/// Simple object pool for List&lt;CharTemplate&gt; to reduce GC pressure
/// </summary>
public static class ListPool
{
    private static readonly ConcurrentQueue<List<CharTemplate>> Pool = new();
    
    public static List<CharTemplate> Get()
    {
        if (Pool.TryDequeue(out var list))
        {
            return list;
        }
        return new List<CharTemplate>();
    }
    
    public static void Return(List<CharTemplate> list)
    {
        if (list == null) return;
        
        list.Clear();
        Pool.Enqueue(list);
    }
}

/// <summary>
/// Simple object pool for CharTemplate objects to reduce GC pressure
/// </summary>
public static class TemplatePool
{
    private static readonly ConcurrentQueue<CharTemplate> Pool = new();
    
    public static CharTemplate Get()
    {
        if (Pool.TryDequeue(out var template))
        {
            // Reset template to default state
            template.Name = string.Empty;
            template.Gender = default;
            template.Vocation = default;
            template.CharId = default;
            template.MapId = default;
            template.Position = default;
            template.Direction = default;
            template.MoveSpeed = default;
            template.AttackCastTime = default;
            template.AttackCooldown = default;
            return template;
        }
        return new CharTemplate();
    }
    
    public static void Return(CharTemplate template)
    {
        if (template == null) return;
        
        Pool.Enqueue(template);
    }
}