using Simulation.Core.Abstractions.Ports.Index;
using Simulation.Core.Abstractions.Ports.Map;

namespace Simulation.Core.Abstractions.Adapters.Map;

public sealed class MapIndex : IMapIndex
{
    private readonly Dictionary<int, MapData> _maps = new();

    public void Add(int mapId, MapData map) => _maps[mapId] = map;
    public bool TryGetMap(int mapId, out MapData? mapData) => _maps.TryGetValue(mapId, out mapData);
}