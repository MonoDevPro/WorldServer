using Simulation.Core;
using Simulation.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSimulationCore();

var host = builder.Build();
host.Run();