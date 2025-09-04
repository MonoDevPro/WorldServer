using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Char.Factories;
using Simulation.Application.Ports.Commons.Pools;
using Simulation.Domain.Components;
using Arch.System.SourceGenerator;
using Simulation.Domain.Templates;

namespace Simulation.Application.Systems;

public sealed partial class CharLifecycleSystem(
    World world,
    ICharFactory charFactory,
    IPoolsService pools,
    ILogger<CharLifecycleSystem> logger)
    : BaseSystem<World, float>(world: world)
{
    // A query para OnSpawnRequest não precisa de todos os componentes, pois a lógica
    // foi movida para os helpers que obtêm os dados necessários.
    [Query]
    [All<EnterIntent>]
    [All<CharId, MapId>]
    private void OnSpawnRequest(in Entity entity, in CharId cid, in MapId mid)
    {
        SendEnterSnapshot(mid.Value, cid.Value, entity); // Para o jogador que entrou
        SendCharSnapshot(mid.Value, cid.Value, entity);  // Para os outros jogadores

        // Remove o componente de intenção, pois já foi processado
        World.Remove<EnterIntent>(entity);
    }

    [Query]
    [All<ExitIntent>]
    [All<CharId, MapId>]
    private void OnDespawnRequest(in Entity e, in ExitIntent intent, in CharId cid, in MapId mid)
    {
        // Envia snapshots de saída e dados persistentes antes de destruir a entidade.
        SendExitSnapshot(mid.Value, cid.Value, e);
        //SendSaveDataSnapshot(mid.Value, cid.Value, e);
        
        // Destrói a entidade, liberando seus componentes do mundo ECS.
        World.Destroy(e);

        logger.LogInformation("Despawned CharId {CharId} (Entity {EntityId})", intent.CharId, e.Id);
    }
    
    private void SendEnterSnapshot(int mapId, int charId, Entity newEntity)
    {
        var templates = pools.RentList();
        var charArray = default(CharTemplate[]); // Declara fora do try para ser acessível no finally
        try
        {
            World.Query(charFactory.GetQueryDescription(), (ref Entity otherEntity, ref MapId otherMid) =>
            {
                if (otherMid.Value == mapId && otherEntity != newEntity)
                {
                    var template = pools.RentTemplate();
                    charFactory.UpdateFromRuntime(template, otherEntity, World);
                    templates.Add(template);
                }
            });

            // Aluga o array para enviar os dados
            charArray = pools.RentArray(templates.Count);
            
            templates.CopyTo(charArray, 0);

            // CORREÇÃO CRÍTICA: Cria uma cópia dos dados para o EventBus, não o array poolado
            EventBus.Send(new EnterSnapshot(mapId, charId, charArray));
        }
        finally
        {
            foreach (var template in templates)
            {
                pools.ReturnTemplate(template);
            }
            pools.ReturnList(templates);
            if (charArray != null)
            {
                pools.ReturnArray(charArray);
            }
        }
    }

    private void SendCharSnapshot(int mapId, int charId, Entity entity)
    {
        var template = pools.RentTemplate();
        try
        {
            charFactory.UpdateFromRuntime(template, entity, World);
            EventBus.Send(new CharSnapshot(mapId, charId, template));
        }
        finally
        {
            pools.ReturnTemplate(template);
        }
    }

    private void SendExitSnapshot(int mapId, int charId, Entity entity)
    {
        var template = pools.RentTemplate();
        try
        {
            charFactory.UpdateFromRuntime(template, entity, World);
            EventBus.Send(new ExitSnapshot(mapId, charId, template));
        }
        finally
        {
            // CORREÇÃO: Garante que o objeto seja devolvido mesmo em caso de erro.
            pools.ReturnTemplate(template);
        }
    }
}

