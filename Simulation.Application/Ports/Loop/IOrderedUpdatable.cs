namespace Simulation.Application.Ports.Loop;

/// <summary>
/// Combina IUpdatable com uma ordem de execução.
/// </summary>
public interface IOrderedUpdatable : IUpdatable, IOrderedService { }
