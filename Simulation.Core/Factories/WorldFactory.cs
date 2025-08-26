using Arch.Core;
using Simulation.Core.Abstractions.In.Factories;

namespace Simulation.Core.Factories;

internal sealed class WorldFactory : IWorldFactory
{
    public World Create()
    {
        return World.Create();
    }
}