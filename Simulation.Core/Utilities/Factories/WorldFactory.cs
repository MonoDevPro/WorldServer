using Arch.Core;

namespace Simulation.Core.Utilities.Factories;

internal static class WorldFactory
{
    public static World Create(
        int chunkSizeInBytes = 16_384, 
        int minimumAmountOfEntitiesPerChunk = 100, 
        int archetypeCapacity = 2, 
        int entityCapacity = 64)
    {
        return World.Create(
            entityCapacity, 
            archetypeCapacity, 
            chunkSizeInBytes, 
            minimumAmountOfEntitiesPerChunk);
    }
}