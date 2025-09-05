using Arch.Core;

namespace Simulation.Application.Ports.ECS.Utils;

/// <summary>
/// Fornece entidades pertencentes a um mapa (por MapId).
/// Implementação concreta faz query no World (ou usa índices especializados).
/// </summary>
public interface IMapEntityProvider
{
    /// <summary>Retorna as entidades (players) que atualmente estão no mapa informado.</summary>
    IEnumerable<Entity> GetEntitiesInMap(World world, int mapId);
}