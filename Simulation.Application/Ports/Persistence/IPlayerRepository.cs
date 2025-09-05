using Simulation.Application.Ports.Persistence.Persistence;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Persistence;

/// <summary>
/// Repositório thread-safe para CharTemplate (em memória).
/// Uso típico: consultar template quando um cliente solicita "enter".
/// </summary>
public interface IPlayerRepository : IRepository<int, PlayerTemplate>
{
}