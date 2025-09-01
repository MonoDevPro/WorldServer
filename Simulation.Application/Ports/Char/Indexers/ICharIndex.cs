using Simulation.Application.Ports.Commons.Indexers;

namespace Simulation.Application.Ports.Char.Indexers;

/// <summary>
/// Define um serviço para mapear CharIds (int) para Entidades vivas na simulação.
/// </summary>
public interface ICharIndex : IEntityIndex<int> { }
