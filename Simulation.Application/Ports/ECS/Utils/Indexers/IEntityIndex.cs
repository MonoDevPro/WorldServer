using Arch.Core;
using Simulation.Application.Ports.Commons;

namespace Simulation.Application.Ports.ECS.Utils.Indexers;

public interface IEntityIndex<TKey> : IIndex<TKey, Entity>
    where TKey : notnull
{
    // opcional: se quiser suporte reverso, herde também IReverseIndex<TKey, Entity>
}