using Arch.Core;

namespace Simulation.Core.Utilities;

public interface IWorldFactory
{
    World Create(int entityCapacity = 10_000);
}

public sealed class DefaultWorldFactory : IWorldFactory
{
    public World Create(int entityCapacity = 10_000)
    {
        return World.Create(entityCapacity);
    }
}

public static class WorldFactory
{
    public static World CreateDefault(int entityCapacity = 10_000) => new DefaultWorldFactory().Create(entityCapacity);
}
