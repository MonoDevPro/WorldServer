using Arch.Core;
using Simulation.Application.Ports.ECS;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.Services.ECS.Utils.Factories;

/// <summary>
/// Define e aplica a composição de uma entidade Mapa no ECS.
/// Segue o mesmo padrão da CharFactoryHelper.
/// </summary>
public class MapFactoryHelper : IFactoryHelper<MapTemplate>
{
    private static readonly ComponentType[] ArchetypeComponents =
    [
        Component<MapId>.ComponentType,
        Component<MapSize>.ComponentType,
        Component<MapFlags>.ComponentType
    ];

    public ComponentType[] GetArchetype() => ArchetypeComponents;

    public void PopulateComponents(MapTemplate data, Span<Action<World, Entity>> setters)
    {
        setters[0] = (world, e) => world.Set(e, new MapId { Value = data.MapId });
        setters[1] = (world, e) => world.Set(e, new MapSize { Width = data.Width, Height = data.Height });
        setters[2] = (world, e) => world.Set(e, new MapFlags { UsePadded = data.UsePadded });
    }

    public void ApplyTo(World world, Entity e, MapTemplate data)
    {
        world.Set<
            MapId,
            MapSize,
            MapFlags
        >(e,
            new MapId { Value = data.MapId },
            new MapSize { Width = data.Width, Height = data.Height },
            new MapFlags { UsePadded = data.UsePadded }
        );
    }
}