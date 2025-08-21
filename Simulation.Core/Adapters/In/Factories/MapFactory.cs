using Simulation.Core.Abstractions.In.Factories;
using Simulation.Core.Components;

namespace Simulation.Core.Adapters.In.Factories;

public class MapFactory : IMapFactory
{
    public MapRef CreateMap(int mapId)
    {
        if (mapId <= 0)
        {
            throw new ArgumentException("Map ID must be a positive integer.", nameof(mapId));
        }

        return new MapRef { MapId = mapId };
    }

    public void DeleteMap(int mapId)
    {
        if (mapId <= 0)
        {
            throw new ArgumentException("Map ID must be a positive integer.", nameof(mapId));
        }

        // Logic to delete the map with the specified ID would go here.
        // This is a placeholder as the actual deletion logic depends on the context of the application.
        // For example, it might involve removing the map from a collection or database.
    }
}