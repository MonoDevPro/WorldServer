using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Components;

namespace Simulation.Core.Systems;

public sealed partial class AttackSystem(World world) : BaseSystem<World, float>(world)
{
    public readonly record struct Attack(Entity Attacker, Entity Target);

    /// <summary>
    /// Inicia uma tentativa de ataque.
    /// </summary>
    public bool Apply(in Attack cmd)
    {
        // Validações básicas
        if (!World.IsAlive(cmd.Attacker) || !World.IsAlive(cmd.Target))
            return false;

        // O atacante precisa dos componentes de ataque
        if (!World.Has<AttackStats>(cmd.Attacker))
            return false;

        ref var state = ref World.AddOrGet<AttackState>(cmd.Attacker);

        // Só pode iniciar um ataque se estiver pronto
        if (state.Phase != AttackPhase.Ready)
            return false;

        // Inicia o ataque
        var stats = World.Get<AttackStats>(cmd.Attacker);
        state.Phase = AttackPhase.Casting;
        state.Timer = stats.Duration; // Inicia o cronômetro da duração do ataque

        World.Add(cmd.Attacker, new Target { Entity = cmd.Target });
        
        // TODO: Aqui você poderia emitir um evento "AttackStarted" para outros sistemas (ex: Animação)
        
        return true;
    }
    
    /// <summary>
    /// Processa entidades que estão na fase de "casting".
    /// </summary>
    [Query]
    [All<AttackState, AttackStats, Target>]
    private void ProcessCasting([Data] in float dt, in Entity entity, ref AttackState state, ref AttackStats stats, ref Target target)
    {
        if (state.Phase != AttackPhase.Casting) return;
        
        state.Timer -= dt;

        // Duração terminou, o ataque acontece agora!
        if (state.Timer <= 0)
        {
            // --- LÓGICA DO DANO/EFEITO DO ATAQUE ACONTECE AQUI ---
            // Exemplo: World.Create(new DamageEvent(target.Entity, 10));
            // Por enquanto, vamos apenas simular que aconteceu.
            
            // TODO: Emitir um evento "AttackHit" ou "DamageApplied"

            // Após o ataque, entra em cooldown
            state.Phase = AttackPhase.OnCooldown;
            state.Timer = stats.Cooldown; // Inicia o cronômetro de recarga
            
            // Remove o alvo, pois a ação de ataque já foi concluída
            World.Remove<Target>(entity);
        }
    }

    /// <summary>
    /// Processa entidades que estão em tempo de recarga.
    /// </summary>
    [Query]
    [All<AttackState>]
    private void ProcessCooldown([Data] in float dt, ref AttackState state)
    {
        if (state.Phase != AttackPhase.OnCooldown) return;

        state.Timer -= dt;
        
        if (state.Timer <= 0)
        {
            // Recarga terminada, a entidade está pronta para atacar novamente
            state.Phase = AttackPhase.Ready;
            state.Timer = 0f;
        }
    }
}