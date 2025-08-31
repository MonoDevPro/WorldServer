using Simulation.Core.Abstractions.Adapters.Map;

namespace Simulation.Core.Abstractions.Ports.Map;

public interface IMapIndex
{
    void Add(int mapId, MapData map);
    bool TryGetMap(int mapId, out MapData? mapData);
}