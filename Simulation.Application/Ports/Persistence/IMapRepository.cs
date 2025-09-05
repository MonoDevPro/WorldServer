using Simulation.Application.Ports.Persistence.Persistence;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Persistence;

/// <summary>
/// Define um serviço para acessar os dados base (persistidos) dos personagens.
/// </summary>
public interface IMapRepository : IRepository<int, MapTemplate> { }
