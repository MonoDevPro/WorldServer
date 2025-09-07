namespace Simulation.Application.Ports.Loop;

/// <summary>
/// Define um serviço que tem uma ordem de execução explícita.
/// </summary>
public interface IOrderedService
{
    /// <summary>
    /// A prioridade de execução. Menores valores são executados primeiro.
    /// </summary>
    int Order { get; }
}