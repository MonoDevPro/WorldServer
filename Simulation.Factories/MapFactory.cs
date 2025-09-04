using Arch.Buffer;
using Arch.Core;
using Simulation.Application.Ports.Map.Factories;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Factories;

/// <summary>
/// Implementa a fábrica e os provedores para a entidade Mapa.
/// Serve como o ponto único de verdade para a definição do que constitui um Mapa no ECS.
/// </summary>
public class MapFactory(CommandBuffer buffer) : IMapFactory
{
    // 1. A dependência (o array de componentes) é declarada primeiro.
    // Esta é a ÚNICA fonte da verdade para a estrutura de um mapa.
    private static readonly ComponentType[] ArchetypeComponents =
    [
        Component<MapId>.ComponentType, 
        Component<MapSize>.ComponentType, 
        Component<MapFlags>.ComponentType
    ];
    
    // 2. O campo que a utiliza (a QueryDescription) vem em seguida.
    private static readonly QueryDescription QueryDesc = new(all: ArchetypeComponents);

    /// <summary>
    /// Responsabilidade principal: Agendar a criação de uma entidade Mapa no CommandBuffer.
    /// </summary>
    public Entity Create(MapTemplate data)
    {
        var entity = buffer.Create(ArchetypeComponents);
        buffer.Set(entity, new MapId { Value = data.MapId });
        buffer.Set(entity, new MapSize { Width = data.Width, Height = data.Height });
        buffer.Set(entity, new MapFlags { UsePadded = data.UsePadded });
        return entity;
    }

    /// <summary>
    /// Implementa a interface IArchetypeProvider.
    /// Retorna a definição central da estrutura de um Mapa.
    /// </summary>
    public ComponentType[] GetArchetypeComponents()
    {
        return ArchetypeComponents;
    }

    /// <summary>
    /// Implementa a interface IQueryProvider.
    /// Retorna a QueryDescription central para buscar todas as entidades Mapa.
    /// </summary>
    public QueryDescription GetQueryDescription()
    {
        return QueryDesc;
    }
}
