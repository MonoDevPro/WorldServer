using System.Collections.Concurrent;
using Arch.Core;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Commons;

namespace Simulation.Core.Utilities;

/// <summary>
/// Uma implementação de Spatial Hash Grid otimizada para ArchECS, focada em baixa alocação de memória (low GC).
/// Armazena referências de Entidades para consultas espaciais rápidas.
/// </summary>
public sealed class SpatialHashGrid
{
    // Pool para reutilizar listas de resultados, evitando alocações em queries.
    private static readonly ConcurrentQueue<List<Entity>> ListPool = new();

    private sealed class MapIndex
    {
        // Célula (grid) -> Lista de entidades naquela célula.
        public readonly Dictionary<GameVector2, List<Entity>> Cells = new();
        // Entidade -> Célula (para remoção/atualização rápida).
        public readonly Dictionary<Entity, GameVector2> EntityToCell = new();
    }

    private readonly Dictionary<int, MapIndex> _maps = new();
    private readonly int _cellSize;

    /// <param name="cellSize">O tamanho de cada célula do grid. Um valor de 16-32 costuma ser um bom começo.</param>
    public SpatialHashGrid(int cellSize = 16)
    {
        if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize), "O tamanho da célula deve ser positivo.");
        _cellSize = cellSize;
    }

    private MapIndex GetOrCreateMap(int mapId)
    {
        if (!_maps.TryGetValue(mapId, out var idx))
        {
            idx = new MapIndex();
            _maps[mapId] = idx;
        }
        return idx;
    }

    private GameVector2 PositionToCell(GameVector2 pos)
    {
        // Divisão inteira que se comporta como Math.Floor para coordenadas negativas.
        int cx = pos.X >= 0 ? pos.X / _cellSize : (pos.X - _cellSize + 1) / _cellSize;
        int cy = pos.Y >= 0 ? pos.Y / _cellSize : (pos.Y - _cellSize + 1) / _cellSize;
        return new GameVector2(cx, cy);
    }

    /// <summary>
    /// Atualiza a posição de uma entidade no grid. Deve ser chamado quando uma entidade muda de célula.
    /// </summary>
    public void Update(Entity entity, int mapId, GameVector2 newPosition)
    {
        var idx = GetOrCreateMap(mapId);
        var newCell = PositionToCell(newPosition);

        // Se a entidade já está no grid, verifica se a célula mudou.
        if (idx.EntityToCell.TryGetValue(entity, out var oldCell))
        {
            if (oldCell.Equals(newCell)) return; // Continua na mesma célula, nada a fazer.

            // Remove da célula antiga.
            if (idx.Cells.TryGetValue(oldCell, out var oldList))
            {
                oldList.Remove(entity);
                if (oldList.Count == 0) idx.Cells.Remove(oldCell);
            }
        }

        // Adiciona à nova célula.
        if (!idx.Cells.TryGetValue(newCell, out var newList))
        {
            newList = new List<Entity>();
            idx.Cells[newCell] = newList;
        }
        newList.Add(entity);
        idx.EntityToCell[entity] = newCell;
    }

    /// <summary>
    /// Remove uma entidade do grid. Deve ser chamado quando uma entidade é destruída.
    /// </summary>
    public void Remove(Entity entity, int mapId)
    {
        if (!_maps.TryGetValue(mapId, out var idx)) return;

        if (idx.EntityToCell.TryGetValue(entity, out var cell))
        {
            if (idx.Cells.TryGetValue(cell, out var list))
            {
                list.Remove(entity);
                if (list.Count == 0) idx.Cells.Remove(cell);
            }
            idx.EntityToCell.Remove(entity);
        }
    }

    /// <summary>
    /// Obtém uma lista de entidades candidatas dentro de um raio a partir de um ponto central.
    /// IMPORTANTE: A lista retornada é do pool e DEVE ser retornada usando ReturnListToPool().
    /// </summary>
    public List<Entity> QueryRadius(int mapId, GameVector2 center, float radius)
    {
        var resultList = GetListFromPool();
        if (!_maps.TryGetValue(mapId, out var idx)) return resultList;

        var minCell = PositionToCell(new GameVector2((int)(center.X - radius), (int)(center.Y - radius)));
        var maxCell = PositionToCell(new GameVector2((int)(center.X + radius), (int)(center.Y + radius)));

        for (int cx = minCell.X; cx <= maxCell.X; cx++)
        {
            for (int cy = minCell.Y; cy <= maxCell.Y; cy++)
            {
                if (idx.Cells.TryGetValue(new GameVector2(cx, cy), out var cellEntities))
                {
                    resultList.AddRange(cellEntities);
                }
            }
        }
        return resultList;
    }
    
    // --- Gerenciamento do Pool ---
    
    public static List<Entity> GetListFromPool()
    {
        return ListPool.TryDequeue(out var list) ? list : new List<Entity>();
    }

    public static void ReturnListToPool(List<Entity> list)
    {
        list.Clear();
        ListPool.Enqueue(list);
    }
}