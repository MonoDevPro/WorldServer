namespace Simulation.Core.Abstractions.In;

public interface ISimulationService
{
    /// <summary>
    /// Starts the simulation service.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the simulation service.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses the simulation service.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the simulation service.
    /// </summary>
    void Resume();
}