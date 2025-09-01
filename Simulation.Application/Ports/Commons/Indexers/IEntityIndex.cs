using Arch.Core;

namespace Simulation.Application.Ports.Commons.Indexers;

public interface IEntityIndex<TKey> : IIndex<TKey, Entity>
    where TKey : notnull
{
    // opcional: se quiser suporte reverso, herde também IReverseIndex<TKey, Entity>
}