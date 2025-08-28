using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Ports;

public interface ISpatialIndex : IDisposable
{
    void Register(int entityId, int mapId, GameCoord tilePos);
    void UpdatePosition(int entityId, int mapId, GameCoord oldPos, GameCoord newPos);
    void Unregister(int entityId, int mapId);
    void QueryAABB(int mapId, int minX, int minY, int maxX, int maxY, Action<int> visitor);
    void QueryRadius(int mapId, GameCoord center, int radius, Action<int> visitor);

    // New API
    bool IsRegistered(int entityId);
    int? GetEntityMap(int entityId);

    // Batch (pending) API
    void EnqueueUpdate(int entityId, int mapId, GameCoord oldPos, GameCoord newPos);
    void Flush(); // apply all enqueued updates
}