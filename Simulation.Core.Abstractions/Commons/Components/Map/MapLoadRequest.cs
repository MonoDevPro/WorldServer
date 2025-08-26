namespace Simulation.Core.Abstractions.Commons.Components.Map;

/// <summary>
/// Um componente "comando" que sinaliza a intenção de carregar um mapa específico.
/// </summary>
public struct MapLoadRequest
{
    public int MapId;
}