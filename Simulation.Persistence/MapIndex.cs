using Simulation.Application.Ports.Map;
using Simulation.Application.Services;

namespace Simulation.Persistence;

public sealed class MapIndex : IMapIndex
{
    private readonly Dictionary<int, MapService> _maps = new();

    public void Add(int mapId, MapService map) => _maps[mapId] = map;
    public bool TryGetMap(int mapId, out MapService? mapData) => _maps.TryGetValue(mapId, out mapData);
}