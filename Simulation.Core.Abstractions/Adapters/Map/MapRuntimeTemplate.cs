using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters.Data;

public class MapRuntimeTemplate(MapTemplate template)
{
    public MapId MapId = new MapId { Value = template.MapId };
    public MapSize MapSize { get; set; } = new(new GameSize(0,0));
    public MapFlags Flags { get; set; } = new(UsePadded: false);
}