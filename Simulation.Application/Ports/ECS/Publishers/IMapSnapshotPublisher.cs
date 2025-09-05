using Simulation.Application.DTOs;
using Simulation.Application.DTOs.Snapshots;

namespace Simulation.Application.Ports.ECS.Publishers;

public interface IMapSnapshotPublisher : IDisposable
{
    void Publish(in LoadMapSnapshot snapshot);
    void Publish(in UnloadMapSnapshot snapshot);
}