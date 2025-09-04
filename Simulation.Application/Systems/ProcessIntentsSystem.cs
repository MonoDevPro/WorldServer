using Arch.Buffer;
using Arch.Core;
using Arch.System;

namespace Simulation.Application.Systems;

public sealed class ProcessIntentsSystem(World world, CommandBuffer buffer) : BaseSystem<World, float>(world)
{
    public override void Update(in float t)
    {
        buffer.Playback(World, dispose: true);
        //base.Update(t);
    }
}