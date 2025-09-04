using Arch.Core;

namespace Simulation.Application.Ports.Commons.Factories;

public interface IArchetypeProvider<TTemplate>
{
    ComponentType[] GetArchetypeComponents();
}