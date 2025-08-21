using Simulation.Core.Abstractions.In;

namespace Simulation.Core.Adapters.In;

public class SimulationConfiguration
{
    public int MaxPlayers { get; set; } = 100;
    public int MaxEntities { get; set; } = 1000;
    public int MaxMaps { get; set; } = 10;

    public override string ToString()
    {
        return $"SimulationConfiguration(" +
               $"MaxPlayers: {MaxPlayers}, " +
               $"MaxEntities: {MaxEntities}, " +
               $"MaxMaps: {MaxMaps})";
    }
}

public class SimulationService(SimulationConfiguration configuration) : ISimulationService
{
    private bool _isRunning;
    
    public void Start()
    {
        if (_isRunning)
            throw new InvalidOperationException("Simulation is already running.");

        // Initialize simulation components here
        _isRunning = true;
        Console.WriteLine("Simulation started with configuration:");
    }

    public void Stop()
    {
        if (!_isRunning)
            throw new InvalidOperationException("Simulation is not running.");

        // Clean up simulation components here
        _isRunning = false;
        Console.WriteLine("Simulation stopped.");
    }

    public void Pause()
    {
        throw new NotImplementedException();
    }

    public void Resume()
    {
        throw new NotImplementedException();
    }
}