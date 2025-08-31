using Arch.Core;

namespace Simulation.Core.Abstractions.Ports.Index;

/// <summary>
/// Interface para um índice que mapeia uma chave externa (como CharId) para uma Entity do ArchECS.
/// </summary>
public interface IEntityIndex<TKey> where TKey : notnull
{
    void Register(TKey key, Entity entity);
    bool Unregister(TKey key);
    bool TryGet(TKey key, out Entity entity);
}