using Simulation.Core.Abstractions.Adapters.Data;

namespace Simulation.Core.Abstractions.Ports;

public interface IMapLoaderSystem
{
    void EnqueueMapData(MapData mapData);
}