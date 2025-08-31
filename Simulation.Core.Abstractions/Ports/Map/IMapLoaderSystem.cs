using Simulation.Core.Abstractions.Adapters.Map;

namespace Simulation.Core.Abstractions.Ports.Map;

public interface IMapLoaderSystem
{
    void EnqueueMapData(MapData mapData);
}