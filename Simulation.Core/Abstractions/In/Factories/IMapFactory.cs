using Simulation.Core.Components;

namespace Simulation.Core.Abstractions.In.Factories;

public interface IMapFactory
{
    /// <summary>
    /// Creates a new map with the specified ID.
    /// </summary>
    /// <param name="mapId">The ID of the map to create.</param>
    /// <returns>A reference to the created map.</returns>
    MapRef CreateMap(int mapId);

    /// <summary>
    /// Deletes the map with the specified ID.
    /// </summary>
    /// <param name="mapId">The ID of the map to delete.</param>
    void DeleteMap(int mapId);
}