using Simulation.Application.DTOs;

namespace Simulation.Application.Ports.Map;

public interface IMapSnapshotPublisher
{
    void Publish(in LoadMapSnapshot snapshot);
    void Publish(in UnloadMapSnapshot snapshot);
}