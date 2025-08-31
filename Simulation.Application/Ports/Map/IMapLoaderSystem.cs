using Simulation.Application.Services;

namespace Simulation.Application.Ports.Map;

public interface IMapLoaderSystem
{
    void EnqueueMapData(MapService mapService);
}