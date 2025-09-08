using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Domain;
using Simulation.ECS.Services;

// Onde está o MapManagerService

namespace Simulation.ECS.Systems;

/// <summary>
/// Processa as intenções de teleporte, validando o destino e
/// atualizando a posição da entidade, além de aplicar um tempo de recarga.
/// </summary>
public sealed partial class TeleportSystem(World world, MapManagerService mapManager)
    : BaseSystem<World, float>(world)
{
    // A duração padrão do cooldown após um teleporte bem-sucedido.
    private const float TeleportCooldownDuration = 5.0f; // 5 segundos

    // Query para encontrar entidades que querem se teleportar e NÃO estão em cooldown.
    [Query]
    [All<Position, MapId, TeleportIntent>]
    [None<TeleportCooldown>]
    private void ExecuteTeleport(in Entity entity, ref Position pos, in MapId mapId, in TeleportIntent intent)
    {
        // 1. Validação do Destino:
        // Usa o MapManagerService para verificar se a célula de destino é válida (não está bloqueada).
        if (mapManager.IsTileBlocked(mapId.Value, intent.TargetPosition))
        {
            // Opcional: Enviar uma notificação de falha para o jogador.
            // Por agora, simplesmente ignoramos a intenção.
        }
        else
        {
            // 2. Executar o Teleporte:
            // A ação é válida, então atualizamos a posição da entidade instantaneamente.
            pos = intent.TargetPosition;

            // 3. Aplicar Cooldown:
            // Adiciona o componente de cooldown para impedir o uso imediato da habilidade novamente.
            World.Add(entity, new TeleportCooldown { CooldownRemaining = TeleportCooldownDuration });
        }

        // 4. Remover a Intenção:
        // A intenção foi processada (com sucesso ou falha), então ela deve ser removida.
        World.Remove<TeleportIntent>(entity);
    }
}