using Arch.Core;
using Simulation.Application.Ports.Commons;

namespace Simulation.Application.Ports.ECS.Utils.Indexers;

/// <summary>
/// Define um serviço para mapear CharIds (int) para Entidades vivas na simulação.
/// </summary>
public interface IPlayerIndex : IEntityIndex<int>, IReverseIndex<int, Entity>
{
}
