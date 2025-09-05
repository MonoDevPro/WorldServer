using Arch.Core;

namespace Simulation.Application.Ports.ECS;

public interface IFactoryHelper<in TTemplate>
    where TTemplate : class
{
    ComponentType[] GetArchetype();
    void PopulateComponents(TTemplate data, Span<Action<World, Entity>> setters);
    void ApplyTo(World world, Entity entity, TTemplate data);
}