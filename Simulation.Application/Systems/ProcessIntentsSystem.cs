using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Simulation.Application.Services;

namespace Simulation.Application.Systems;
public sealed class ProcessIntentsSystem(World world, DoubleBufferedCommandBuffer doubleBuffer) : BaseSystem<World, float>(world)
{
    public override void Update(in float t)
    {
        // Este call faz swap atômico e processa o buffer que estava ativo anteriormente.
        // Chame uma vez por tick (antes de sistemas que dependem destas entidades).
        doubleBuffer.SwapAndPlayback(World, disposeAfterPlayback: true);
    }
}
