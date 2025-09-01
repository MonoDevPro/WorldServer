using Simulation.Application.Ports.Commons.Persistence;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Map.Indexers;

/// <summary>
/// Define um serviço para acessar os dados base (persistidos) dos personagens.
/// </summary>
public interface IMapTemplateRepository : IRepository<int, MapTemplate> { }
