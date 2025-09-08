using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Domain;
using Simulation.ECS.Utils;

namespace Simulation.ECS.Systems.Index;

public sealed partial class SpatialIndexSystem(World world, int mapWidth, int mapHeight) 
    : BaseSystem<World, float>(world)
{
    // A instância do seu índice espacial
    private readonly QuadTreeSpatial _spatialIndex = new(0, 0, mapWidth, mapHeight);
    
    // Propriedade pública para que outros sistemas possam acessar o índice
    public ISpatialIndex Index => _spatialIndex;

    [Query]
    [All<InSpatialIndex>]
    private void RemoveFromIndex(in Entity entity)
    {
        if (!World.IsAlive(entity))
        {
            _spatialIndex.Remove(entity);
        }
    }
    
    [Query]
    [All<Position, LastKnownPosition, InSpatialIndex>]
    private void UpdateIndex(in Entity entity, ref Position currentPos, ref LastKnownPosition lastPos)
    {
        if (currentPos.X != lastPos.Value.X || currentPos.Y != lastPos.Value.Y)
        {
            _spatialIndex.Update(entity, currentPos);
            lastPos.Value = currentPos; // Atualiza a última posição conhecida
        }
    }

    [Query]
    [All<Position>]
    [None<InSpatialIndex>]
    private void AddToIndex(in Entity entity, ref Position pos)
    {
        _spatialIndex.Add(entity, pos);
        World.Add<InSpatialIndex>(entity);
        World.Add<LastKnownPosition>(entity, new LastKnownPosition { Value = pos });
    }
    
    [Query]
    [All<Position, InSpatialIndex>]
    [None<LastKnownPosition>]
    private void AddLastKnownPosition(in Entity entity, ref Position pos)
    {
        World.Add<LastKnownPosition>(entity, new LastKnownPosition { Value = pos });
    }
}