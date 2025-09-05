namespace Simulation.Application.Ports.Commons;

/// <summary>
/// Índice genérico que mapeia uma chave externa (ex: CharId) para um valor (ex: Entity).
/// </summary>
public interface IIndex<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    void Register(TKey key, TValue value);
    bool Unregister(TKey key);
    bool TryGet(TKey key, out TValue? value);
}