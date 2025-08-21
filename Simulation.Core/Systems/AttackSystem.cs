using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Commons;
using Simulation.Core.Commons.Enums;
using Simulation.Core.Components;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

/// <summary>
/// Gerencia o ciclo de ataques, desde o início (casting), resolução do dano e tempo de recarga (cooldown).
/// </summary>
public sealed partial class AttackSystem(World world, SpatialHashGrid grid) : BaseSystem<World, float>(world)
{
    /// <summary>
    /// Inicia uma tentativa de ataque com base nos parâmetros fornecidos.
    /// </summary>
    public bool Apply(in Requests.Attack cmd)
    {
        if (!World.IsAlive(cmd.Attacker) || !World.Has<AttackStats>(cmd.Attacker))
            return false;

        ref var state = ref World.AddOrGet<AttackState>(cmd.Attacker);
        if (state.Phase != AttackPhase.Ready)
            return false; // Entidade já está atacando ou em cooldown

        // Validações específicas por tipo de ataque
        switch (cmd.Type)
        {
            case AttackType.Melee or AttackType.Ranged:
                if (!World.IsAlive(cmd.TargetEntity)) return false;
                break;
            case AttackType.AreaOfEffect:
                if (cmd.Radius <= 0) return false;
                break;
        }

        // Tudo certo, inicia a fase de casting
        var stats = World.Get<AttackStats>(cmd.Attacker);
        state.Phase = AttackPhase.Casting;
        state.Timer = stats.Duration;
        state.EnteredCooldownThisFrame = false; // reset flag

        // Adiciona o componente de contexto do ataque
        World.Add(cmd.Attacker, new AttackCasting
        {
            Type = cmd.Type,
            TargetEntity = cmd.TargetEntity,
            TargetPosition = cmd.TargetPosition,
            Radius = cmd.Radius
        });
        
        return true;
    }
    
    /// <summary>
    /// Processa entidades que estão preparando um ataque (fase de casting).
    /// </summary>
    [Query]
    [All<AttackState, AttackStats, AttackCasting>]
    private void ProcessCasting([Data] in float dt, in Entity entity, ref AttackState state, ref AttackStats stats, ref AttackCasting castInfo)
    {
        if (state.Phase != AttackPhase.Casting) return;
        
        state.Timer -= dt;

        if (state.Timer <= 0)
        {
            // O tempo de casting terminou. O ataque é resolvido AGORA!
            ResolveAttack(in entity, in castInfo);

            // Inicia o cooldown
            state.Phase = AttackPhase.OnCooldown;
            state.Timer = stats.Cooldown;
            state.EnteredCooldownThisFrame = true; // marca para não decrementar neste frame
            
            // Remove o componente de contexto, limpando o estado
            World.Remove<AttackCasting>(entity);
        }
    }

    /// <summary>
    /// Processa entidades que estão em tempo de recarga.
    /// </summary>
    [Query]
    [All<AttackState>]
    [None<AttackCasting>] // Garante que esta query não rode em quem ainda está em casting
    private void ProcessCooldown([Data] in float dt, ref AttackState state)
    {
        if (state.Phase != AttackPhase.OnCooldown) return;

        if (state.EnteredCooldownThisFrame)
        {
            // Pula a subtração neste frame para evitar consumir dt duplo na transição.
            state.EnteredCooldownThisFrame = false;
            return;
        }

        state.Timer -= dt;
        
        if (state.Timer <= 0)
        {
            state.Phase = AttackPhase.Ready;
            state.Timer = 0f;
        }
    }

    /// <summary>
    /// Contém a lógica de dano/efeito para cada tipo de ataque.
    /// </summary>
    private void ResolveAttack(in Entity attacker, in AttackCasting castInfo)
    {
        var attackerMap = World.Get<MapRef>(attacker);
        
        switch (castInfo.Type)
        {
            case AttackType.Melee:
            case AttackType.Ranged:
                if (World.IsAlive(castInfo.TargetEntity))
                {
                    // TODO: Aplicar dano/efeito ao castInfo.TargetEntity
                    // Ex: World.Create(new DamageEvent(castInfo.TargetEntity, 20));
                    Console.WriteLine($"Entidade {attacker.Id} atacou {castInfo.TargetEntity.Id}!");
                }
                break;

            case AttackType.AreaOfEffect:
                var targets = grid.QueryRadius(attackerMap.MapId, castInfo.TargetPosition, castInfo.Radius);
                
                Console.WriteLine($"Entidade {attacker.Id} usou ataque em área em {castInfo.TargetPosition} com {targets.Count} alvos potenciais.");
                foreach (var target in targets)
                {
                    if (target == attacker || !World.IsAlive(target)) continue;
                    // TODO: Aplicar dano/efeito ao target
                    // Ex: World.Create(new DamageEvent(target, 15));
                }
                
                // ESSENCIAL: Devolver a lista ao pool para evitar alocação de memória!
                SpatialHashGrid.ReturnListToPool(targets);
                break;
        }
    }
}