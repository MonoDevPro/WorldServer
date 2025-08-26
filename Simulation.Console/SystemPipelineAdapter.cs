using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core;
using Simulation.Network;

namespace Simulation.Console;

public class SystemPipelineAdapter : SimulationPipeline
{
    public SystemPipelineAdapter(IServiceProvider provider) 
        : base(provider)
    {
        var logger = provider.GetRequiredService<ILogger<SystemPipelineAdapter>>();
        var net = provider.GetRequiredService<NetworkSystem>();
        
        var list = new List<BaseSystem<World, float>> { net };
        list.AddRange(this);
        Clear();
        AddRange(list);
        
        logger.LogInformation("Sistemas na pipeline: {Count}", this.Count);
    }
}