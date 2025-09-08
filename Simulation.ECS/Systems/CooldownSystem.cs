using Arch.Core;
using Arch.System;
using Simulation.Domain;

namespace Simulation.ECS.Systems;

/// <summary>
/// Um sistema genérico que processa a contagem regressiva de vários
/// tipos de cooldowns a cada tick.
/// </summary>
public sealed partial class CooldownSystem(World world) : BaseSystem<World, float>(world)
{
    // Query para o cooldown de teleporte
    [Query]
    private void UpdateTeleportCooldowns([Data] in float dt, in Entity entity, ref TeleportCooldown cooldown)
    {
        cooldown.CooldownRemaining -= dt;
        if (cooldown.CooldownRemaining <= 0f)
        {
            // Cooldown terminou, remove o componente para liberar a habilidade.
            World.Remove<TeleportCooldown>(entity);
        }
    }
    
    // Você poderia adicionar mais queries aqui para outros tipos de cooldowns.
    // Exemplo:
    // [Query]
    // private void UpdateFireballCooldowns(in Entity entity, ref FireballCooldown cooldown, in float deltaTime)
    // { ... }
}