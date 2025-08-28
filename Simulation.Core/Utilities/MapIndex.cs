using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Utilities;

public sealed class MapIndex : IMapIndex
{
    private readonly Dictionary<int, MapData> _maps = new();

    public void Add(int mapId, MapData map) => _maps[mapId] = map;
    public bool TryGetMap(int mapId, out MapData? mapData) => _maps.TryGetValue(mapId, out mapData);
}