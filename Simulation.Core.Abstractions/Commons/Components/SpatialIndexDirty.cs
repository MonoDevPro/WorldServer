using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Commons.Components;

/// <summary>
/// Marca que a entidade teve sua posição espacial alterada e precisa propagar essa alteração
/// para o SpatialIndex em batch no fim do frame.
/// </summary>
public struct SpatialIndexDirty 
{
    public GameVector2 Old;
    public GameVector2 New;
    public int MapId; 
}
