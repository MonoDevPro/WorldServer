using System;
using Arch.Core;
using Arch.Core.Extensions.Dangerous;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Components.Attack;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

/// <summary>
/// Gerencia o ciclo de ataques, desde o início (casting), resolução do dano e tempo de recarga (cooldown).
/// </summary>
public sealed partial class AttackSystem(World world, ISpatialIndex grid, IEntityIndex entityIndex) : BaseSystem<World, float>(world)
{
    private readonly ISpatialIndex _grid = grid;

    // 1. Iniciação do Ataque: Procura por entidades prontas que receberam a ordem de atacar.
    [Query]
    [All<AttackIntent>]
    [All<AttackSpeed>]
    [None<AttackAction>] // Garante que não estamos atacando já ou em cooldown
    private void ProcessAttackIntent(in Entity e, in AttackIntent cmd, in AttackSpeed speed)
    {
        // Cria o componente de ação de ataque com base na velocidade de ataque
        var attackAction = new AttackAction
        {
            Duration = speed.CastTime,
            Remaining = speed.CastTime,
            Cooldown = speed.Cooldown,
            CooldownRemaining = 0f
        };

        // Cria o snapshot de ataque para enviar aos clientes
        var attackSnapshot = new AttackSnapshot(cmd.AttackerCharId);

        // Adiciona o componente de ação de ataque à entidade atacante
        World.Add<AttackAction>(e, attackAction);

        // Adiciona o componente de snapshot para resposta aos clientes
        World.Add<AttackSnapshot>(e, attackSnapshot);
    }

    // Agora processamos AttackAction juntamente com posição e referência ao mapa
    [Query]
    [All<AttackAction>]
    [All<AttackIntent>]   // queremos acesso à intenção original (ex.: alcance, tipo)
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

                // Attack finished — hora de resolver efeitos (achar alvos)
                var range = 1; // supondo que AttackIntent tem Range; se não tiver, substitua por constante

                // Proteção caso não exista Range
                //if (range <= 0) range = 1;

                Console.WriteLine($"Attack finished for entity {e.Id} at {pos.Position}. Searching targets in range {range}");

                // Query spatial para achar entidades no radius (AABB -> caller pode filtrar distância real se quiser)
                var id = e.Id;
                _grid.QueryRadius(map.MapId, pos.Position, range, targetEid =>
                {
                    if (targetEid == id) return; // ignorar self

                    if (!entityIndex.TryGetByEntityId(targetEid, out var targetEntity))
                        return; // entidade não encontrada (pode ter sido removida)

                    // TODO: aplicar lógica de dano, checar afiliações, esquivar, adicionar snapshots, etc.
                    // Exemplo genérico: logar
                    Console.WriteLine($" -> potential hit: entity {targetEid}");

                    // Exemplo seguro (se você tiver componentes de vida/dano):
                    // if (World.Has<Health>(targetEntity)) { World.Get<Health>(targetEntity).Value -= computedDamage; }
                    //
                    // Ou emitir um componente "IncomingDamage" / snapshot para rede.
                });

                // disparar evento AttackFinished(e) / aplicar efeitos finais
            }
            // aplicar efeitos durante a execução se necessário
        }
        else if (a.CooldownRemaining > 0f)
        {
            a.CooldownRemaining -= dt;
            if (a.CooldownRemaining <= 0f)
            {
                // cooldown terminou — remover componente ou resetar
                World.Remove<AttackAction>(e);
            }
        }
    }
}
