using Arch.Core;

namespace Simulation.Core.Abstractions.Out;

/// <summary>
/// Índice para resolver entidades ECS por um identificador estável (ex.: CharacterId).
/// Exposto como porta de saída para que a rede consiga mapear mensagens para entidades.
/// </summary>
public interface IEntityIndex
{
    void Register(int characterId, in Entity entity);
    void UnregisterByCharId(int characterId);
    void UnregisterEntity(in Entity entity);
    bool TryGetByCharId(int characterId, out Entity entity);
    bool TryGetByEntityId(int entityId, out Entity entity);
}
