namespace Simulation.Application.Ports.Loop;

/// <summary>
/// Combina IInitializable com uma ordem de execução.
/// </summary>
public interface IOrderedInitializable : IInitializable, IOrderedService { }