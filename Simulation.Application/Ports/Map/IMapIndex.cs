using Simulation.Application.Services;

namespace Simulation.Application.Ports.Map;

public interface IMapIndex
{
    void Add(int mapId, MapService map);
    bool TryGetMap(int mapId, out MapService? mapData);
}