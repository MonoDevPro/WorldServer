using Arch.Core;

namespace Simulation.Core.Abstractions.Ports;

public interface IEntityIndex
{
    void Register(int characterId, in Entity entity);
    void UnregisterByCharId(int characterId);
    void UnregisterEntity(in Entity entity);
    bool TryGetByCharId(int characterId, out Entity entity);
    bool TryGetByEntityId(int entityId, out Entity entity);
}