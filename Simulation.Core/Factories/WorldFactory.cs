using Arch.Core;
using Simulation.Core.Abstractions.In.Factories;

namespace Simulation.Core.Factories;

internal sealed class WorldFactory : IWorldFactory
{
    public World Create(int entityCapacity = 10_000)
    {
        return World.Create(entityCapacity);
    }
}