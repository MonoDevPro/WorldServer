namespace Simulation.Application.Ports.Commons;

/// <summary>
/// Operações opcionais de índice que permitem remover / procurar pela *valor* (reverse lookup).
/// </summary>
public interface IReverseIndex<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    bool UnregisterValue(TValue value);
    bool TryGetKey(TValue value, out TKey? key);
}