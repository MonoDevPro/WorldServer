using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Commons; // Adicionado
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Systems;

public sealed partial class AttackSystem(
    World world,
    ISpatialIndex grid,
    IEntityIndex entityIndex,
    ILogger<AttackSystem> logger)
    : BaseSystem<World, float>(world)
{

    [Query]
    [All<AttackIntent>]
    [All<AttackStats>]
    [None<AttackAction>]
    private void ProcessAttackIntent(in Entity e, in AttackIntent cmd, in AttackStats stats)
    {
        var attackAction = new AttackAction
        {
            Duration = stats.CastTime,
            Remaining = stats.CastTime,
            Cooldown = stats.Cooldown,
            CooldownRemaining = 0f
        };
        var attackSnapshot = new AttackSnapshot(cmd.AttackerCharId);
        
        World.AddRange(e, new Span<object>([attackAction, attackSnapshot]));
        World.Remove<AttackIntent>(e);
        
        logger.LogInformation("Ataque iniciado pela entidade {EntityId} (CharId: {CharId})", e.Id, cmd.AttackerCharId);
    }
    
    [Query]
    [All<AttackAction>]
    [All<Position>]
    [All<MapId>]
    private void ProcessAttackAction([Data] in float dt, in Entity e, ref AttackAction a, in Position pos, in MapId map)
    {
        if (a.Remaining > 0f)
        {
            a.Remaining -= dt;
            if (a.Remaining <= 0f)
            {
                a.Remaining = 0f;
                a.CooldownRemaining = a.Cooldown;

                var range = 1; 
                logger.LogInformation("Ataque da entidade {EntityId} finalizado em {Position}. Procurando alvos no alcance {Range}", e.Id, pos.Value, range);

                var attackerId = e.Id;
                grid.QueryRadius(map.Value, pos.Value, range, targetEid =>
                {
                    if (targetEid == attackerId) return;

                    if (!entityIndex.TryGetByEntityId(targetEid, out var targetEntity))
                        return;
                    
                    logger.LogDebug(" -> Alvo em potencial: entidade {TargetEntityId}", targetEid);
                    
                    // TODO: Lógica de dano
                });
            }
        }
        else if (a.CooldownRemaining > 0f)
        {
            a.CooldownRemaining -= dt;
            if (a.CooldownRemaining <= 0f)
            {
                World.Remove<AttackAction>(e);
            }
        }
    }
}