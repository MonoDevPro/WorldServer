using Simulation.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSimulation();

var host = builder.Build();
host.Run();