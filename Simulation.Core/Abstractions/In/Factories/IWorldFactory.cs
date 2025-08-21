using Arch.Core;

namespace Simulation.Core.Abstractions.In.Factories;

public interface IWorldFactory
{
    World Create(int entityCapacity = 10_000);
}