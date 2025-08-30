using Arch.Core;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters.Char;

public readonly struct InlineGetAllChar(EnterSnapshot) : IForEachWithEntity<CharId>
{
    public void Update(Entity entity, ref CharId t0)
    {
        throw new NotImplementedException();
    }
}