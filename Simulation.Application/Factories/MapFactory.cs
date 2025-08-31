using Arch.Buffer;
using Arch.Core;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.Factories;

public static class MapFactory
{
    private static readonly ComponentType[] MapArchetype =
    {
        Component<MapId>.ComponentType, 
        Component<MapSize>.ComponentType, 
        Component<MapFlags>.ComponentType
    };
    
    private static readonly Signature MapSignature = SignatureBuilder.Create(MapArchetype);
    
    public static QueryDescription QueryDescription = new(all: MapSignature);
    
    public static Entity CreateEntity(CommandBuffer cmd, MapTemplate tpl)
    {
        var entity = cmd.Create(MapArchetype);
        cmd.Set(entity, new MapId { Value = tpl.MapId });
        cmd.Set(entity, new MapSize { Width = tpl.Width, Height = tpl.Height });
        cmd.Set(entity, new MapFlags { UsePadded = tpl.UsePadded });
        return entity;
    }
    
}