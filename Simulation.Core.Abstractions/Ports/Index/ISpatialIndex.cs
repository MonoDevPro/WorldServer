using Arch.Core;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Ports.Index;

/// <summary>
/// Interface para um índice espacial que opera sobre o mundo ECS.
/// </summary>
public interface ISpatialIndex
{
    void Add(Entity entity, Position position);
    void Remove(Entity entity);
    void Update(Entity entity, Position newPosition);
    void Query(Position center, int radius, List<Entity> results);
    List<Entity> Query(Position center, int radius);
}