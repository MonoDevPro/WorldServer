using Arch.Core;
using Simulation.Application.DTOs;

namespace Simulation.Application.Ports.ECS.Utils;

/// <summary>
/// Constrói DTOs de estado (snapshot) a partir de entidades do World.
/// Implementação concreta deve usar World para ler componentes.
/// </summary>
public interface IStateSnapshotBuilder
{
    PlayerState BuildCharState(World world, Entity entity);
}