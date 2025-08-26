using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging; // Adicionado
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Components.Attack;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed partial class AttackSystem : BaseSystem<World, float>
{
    private readonly ISpatialIndex _grid;
    private readonly IEntityIndex _entityIndex;
    private readonly ILogger<AttackSystem> _logger; // Adicionado

    public AttackSystem(World world, ISpatialIndex grid, IEntityIndex entityIndex, ILogger<AttackSystem> logger) : base(world) // Adicionado
    {
        _grid = grid;
        _entityIndex = entityIndex;
        _logger = logger; // Adicionado
    }
    
    [Query]
    [All<AttackIntent>]
    [All<AttackSpeed>]
    [None<AttackAction>]
    private void ProcessAttackIntent(in Entity e, in AttackIntent cmd, in AttackSpeed speed)
    {
        var attackAction = new AttackAction
        {
            Duration = speed.CastTime,
            Remaining = speed.CastTime,
            Cooldown = speed.Cooldown,
            CooldownRemaining = 0f
        };
        var attackSnapshot = new AttackSnapshot(cmd.AttackerCharId);
        
        World.Add<AttackAction>(e, attackAction);
        World.Add<AttackSnapshot>(e, attackSnapshot);
        
        _logger.LogInformation("Ataque iniciado pela entidade {EntityId} (CharId: {CharId})", e.Id, cmd.AttackerCharId);
    }
    
    [Query]
    [All<AttackAction>]
    [All<AttackIntent>]
    [All<TilePosition>]
    [All<MapRef>]
    private void ProcessAttackAction([Data] in float dt, in Entity e, ref AttackAction a, in AttackIntent intent, in TilePosition pos, in MapRef map)
    {
        if (a.Remaining > 0f)
        {
            a.Remaining -= dt;
            if (a.Remaining <= 0f)
            {
                a.Remaining = 0f;
                a.CooldownRemaining = a.Cooldown;

                var range = 1; 
                _logger.LogInformation("Ataque da entidade {EntityId} finalizado em {Position}. Procurando alvos no alcance {Range}", e.Id, pos.Position, range);

                var attackerId = e.Id;
                _grid.QueryRadius(map.MapId, pos.Position, range, targetEid =>
                {
                    if (targetEid == attackerId) return;

                    if (!_entityIndex.TryGetByEntityId(targetEid, out var targetEntity))
                        return;
                    
                    _logger.LogDebug(" -> Alvo em potencial: entidade {TargetEntityId}", targetEid);
                    
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