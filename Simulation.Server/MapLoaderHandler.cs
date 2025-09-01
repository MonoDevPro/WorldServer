using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Map;

namespace Simulation.Server;

public class MapLoaderHandler(ILogger<MapLoaderHandler> logger) : IMapSnapshotPublisher
{
    public void Publish(in LoadMapSnapshot snapshot)
    {
        logger.LogInformation("Loading map {MapId}", snapshot.MapId);
    }

    public void Publish(in UnloadMapSnapshot snapshot)
    {
        logger.LogInformation("Unloading map {MapId}", snapshot.MapId);
    }
}