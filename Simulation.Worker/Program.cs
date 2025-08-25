using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulation.Core;
using Simulation.Worker;
using Simulation.Network;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSimulationCore();
builder.Services.AddSimulationNetwork();

builder.Services.AddSingleton<SistemPipelineAdapter>();
builder.Services.Replace(ServiceDescriptor.Singleton<SimulationPipeline>(a => a.GetRequiredService<SistemPipelineAdapter>()));

var host = builder.Build();
host.Run();

public class SistemPipelineAdapter(IServiceProvider provider, NetworkSystem networkSystem)
    : SimulationPipeline(provider)
{
    public override void Configure()
    {
        // Sobrescreve o pipeline para adicionar o network system no início
        Insert(0, networkSystem);
        
        // Order of systems execution
        base.Configure();
    }
}