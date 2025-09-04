using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Simulation.Domain.Templates;

namespace Simulation.Core.Tests.Utilities;

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
        Pool.Enqueue(template);
    }
}

/// <summary>
/// Wrapper around ArrayPool for CharTemplate arrays
/// </summary>
public static class TemplateArrayPool
{
    private static readonly ArrayPool<CharTemplate> Pool = ArrayPool<CharTemplate>.Shared;
    
    public static CharTemplate[] Get(int minimumLength)
    {
        return Pool.Rent(minimumLength);
    }
    
    public static void Return(CharTemplate[] array)
    {
        Pool.Return(array, clearArray: true);
    }
    
    /// <summary>
    /// Creates a new array with exactly the required size from the pooled array
    /// </summary>
    public static CharTemplate[] CreateExactArray(List<CharTemplate> templates)
    {
        if (templates.Count == 0)
            return [];
            
        var pooledArray = Get(templates.Count);
        try
        {
            var result = new CharTemplate[templates.Count];
            for (int i = 0; i < templates.Count; i++)
            {
                result[i] = templates[i];
            }
            return result;
        }
        finally
        {
            Return(pooledArray);
        }
    }
}