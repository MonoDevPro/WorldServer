using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Domain;
using Simulation.ECS.Systems.Index;

namespace Simulation.ECS.Systems;

public sealed partial class CombatSystem(World world, SpatialIndexSystem spatialIndexSystem)
    : BaseSystem<World, float>(world)
{
    // Query 1: Inicia um novo ataque.
    // Encontra entidades com intenção de atacar, que não estão já atacando ou em cooldown.
    [Query]
    [All<Position, AttackStats, AttackIntent>]
    [None<AttackAction, AttackCooldown>]
    private void StartAttack(in Entity entity, in Position pos, in AttackStats stats, in AttackIntent intent)
    {
        // Valida se o alvo ainda existe e está vivo.
        if (!World.IsAlive(intent.Target) || World.Has<Dead>(intent.Target))
        {
            World.Remove<AttackIntent>(entity);
            return;
        }

        // Valida o alcance do ataque usando o índice espacial.
        ref var targetPosition = ref World.Get<Position>(intent.Target);
        int distanceSq = (pos.X - targetPosition.X) * (pos.X - targetPosition.X) + 
                         (pos.Y - targetPosition.Y) * (pos.Y - targetPosition.Y);
        
        if (distanceSq <= stats.AttackRange * stats.AttackRange)
        {
            // Se o alvo está ao alcance, inicia a ação de ataque.
            World.Add(entity, new AttackAction
            {
                Target = intent.Target,
                CastTimeRemaining = stats.CastTime
            });
        }
        
        // Remove a intenção, pois ela foi processada.
        World.Remove<AttackIntent>(entity);
    }

    // Query 2: Processa ataques que estão em andamento (durante o cast).
    [Query]
    [All<AttackAction, AttackStats>]
    private void ContinueAttack([Data] in float dt, in Entity entity, ref AttackAction action, in AttackStats stats)
    {
        action.CastTimeRemaining -= dt;

        if (action.CastTimeRemaining <= 0f)
        {
            // O tempo de cast terminou. Aplica o dano.
            if (World.IsAlive(action.Target) && !World.Has<Dead>(action.Target))
            {
                // Aplica o dano no alvo.
                ref var targetHealth = ref World.Get<Health>(action.Target);
                targetHealth.Current -= stats.Damage;
            }
            
            // Inicia o cooldown do ataque.
            World.Add(entity, new AttackCooldown { CooldownRemaining = stats.Cooldown });
            
            // Remove a ação de ataque.
            World.Remove<AttackAction>(entity);
        }
    }

    // Query 3: Verifica por entidades que morreram.
    [Query]
    [All<Health>]
    [None<Dead>]
    private void CheckForDeath(in Entity entity, in Health health)
    {
        if (health.Current <= 0)
        {
            // A vida chegou a zero, marca a entidade como morta.
            World.Add<Dead>(entity);
            
            // Outros sistemas podem agora reagir à morte da entidade,
            // como um DeathSystem para remover a entidade do jogo após um tempo,
            // ou um LootSystem para gerar recompensas.
        }
    }
}