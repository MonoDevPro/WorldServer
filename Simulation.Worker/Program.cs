using Simulation.Core;
using Simulation.Worker;
using Simulation.Network;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSimulationCore();
builder.Services.AddSimulationNetwork();

var host = builder.Build();
host.Run();