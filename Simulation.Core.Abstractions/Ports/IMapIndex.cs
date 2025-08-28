using Simulation.Core.Abstractions.Adapters.Data;

namespace Simulation.Core.Abstractions.Ports;

public interface IMapIndex
{
    void Add(int mapId, MapData map);
    bool TryGetMap(int mapId, out MapData? mapData);
}