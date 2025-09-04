using Microsoft.Extensions.ObjectPool;
using Simulation.Application.DTOs;
using Simulation.Domain.Templates;

namespace Simulation.Pooling;

public sealed class CharSaveTemplatePooledPolicy : PooledObjectPolicy<CharSaveTemplate>
{
    public override CharSaveTemplate Create() => new CharSaveTemplate();

    public override bool Return(CharSaveTemplate obj)
    {
        Reset(obj);
        return true;
    }

    private static void Reset(CharSaveTemplate t)
    {
        t.CharId = default;
        t.MapId = default;
        t.Position = default;
        t.Direction = default;
        t.MoveStats = default;
        t.AttackStats = default;
    }
}