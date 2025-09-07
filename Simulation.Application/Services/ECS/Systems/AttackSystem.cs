using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs.Intents;
using Simulation.Application.DTOs.Snapshots;
using Simulation.Application.Ports.ECS.Utils.Indexers;
using Simulation.Domain.Components;

namespace Simulation.Application.Services.ECS.Systems
{
    /// <summary>
    /// Gerencia a lógica de ataques, desde a intenção até a aplicação de dano.
    /// </summary>
    public sealed partial class AttackSystem(World world, ISpatialIndex spatialIndex, ILogger<AttackSystem> logger)
        : BaseSystem<World, float>(world)
    {
        // Usamos uma lista reutilizável para os resultados da query espacial para evitar alocações de memória.
        private readonly List<Entity> _queryResults = new();

        /// <summary>
        /// Processa a intenção de atacar, iniciando a ação de ataque.
        /// </summary>
        [Query]
        [All<AttackIntent, AttackStats>]
        [None<AttackAction>] // Garante que não iniciemos um novo ataque se um já estiver em andamento.
        private void ProcessAttackIntent(in Entity entity, in AttackIntent cmd, in AttackStats stats)
        {
            // If still cooling down from a previous attack, ignore the intent (server authority)
            if (World.Has<AttackAction>(entity))
            {
                World.Remove<AttackIntent>(entity);
                return;
            }
            // 1. Cria a ação de ataque com base nos status do personagem.
            var attackAction = new AttackAction
            {
                Duration = stats.CastTime,
                Remaining = stats.CastTime,
                Cooldown = stats.Cooldown,
                CooldownRemaining = 0f
            };
            World.Add(entity, attackAction);
            
            // 2. Dispara um evento para a rede, notificando que o ataque começou.
            var attackSnapshot = new AttackSnapshot { CharId = cmd.CharId };
            EventBus.Send(in attackSnapshot);
            
            // 3. Remove o componente de intenção, pois já foi processado.
            World.Remove<AttackIntent>(entity);
        
            logger.LogInformation("CharId {id} iniciou um ataque.", cmd.CharId);
        }
    
        /// <summary>
        /// Processa as ações de ataque em andamento.
        /// </summary>
        [Query]
        [All<MapId, AttackAction, Position, Direction>]
        private void ProcessAttackAction([Data] in float dt, in Entity entity, ref AttackAction action, in Position pos, in Direction dir)
        {
            // Se a ação está na fase de "cast time"
            if (action.Remaining > 0f)
            {
                action.Remaining -= dt;
                if (action.Remaining <= 0f)
                {
                    // O ataque "acerta" agora.
                    action.Remaining = 0f;
                    action.CooldownRemaining = action.Cooldown;

                    // Define a área de efeito (ex: um quadrado 3x3 na frente do jogador)
                    var attackCenter = new Position { X = pos.X + dir.X, Y = pos.Y + dir.Y };
                    var attackRadius = 1; // Raio de 1 resulta em uma área 3x3

                    logger.LogDebug("Ataque de {entity} acertou em {pos}. Procurando alvos.", entity, attackCenter);

                    // Limpa a lista de resultados e consulta o índice espacial.
                    _queryResults.Clear();
                    spatialIndex.Query(attackCenter, attackRadius, _queryResults);

                    foreach (var targetEntity in _queryResults)
                    {
                        // Não acerta a si mesmo.
                        if (targetEntity == entity) continue;

                        logger.LogInformation("Alvo encontrado: {target}. Aplicando dano...", targetEntity);

                        // TODO: Aplicar dano ao alvo.
                        // A melhor prática é adicionar um componente de evento, ex:
                        // World.Add(targetEntity, new TakeDamageEvent { Amount = 10, Source = entity });
                        // Outro sistema (ex: HealthSystem) processaria esse evento.
                    }
                }
            }
            // Se a ação está na fase de "cooldown"
            else if (action.CooldownRemaining > 0f)
            {
                action.CooldownRemaining -= dt;
                if (action.CooldownRemaining <= 0f)
                {
                    // Cooldown terminou, remove o componente para permitir um novo ataque.
                    World.Remove<AttackAction>(entity);
                }
            }
        }
    }
}
