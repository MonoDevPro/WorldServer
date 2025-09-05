using System.Runtime.InteropServices;
using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons.Pools;
using Simulation.Domain.Components;
using Arch.System.SourceGenerator;
using Simulation.Application.Ports.Char;
using Simulation.Domain.Templates;

namespace Simulation.Application.Systems;

public sealed partial class CharLifecycleSystem(
    World world,
    ICharFactory charFactory,
    IPoolsService poolsService,
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
        var templates = poolsService.RentList();
        var charArray = default(CharTemplate[]); // Declara fora do try para ser acessível no finally
        try
        {
            World.Query(charFactory.GetQueryDescription(), (ref Entity otherEntity, ref MapId otherMid) =>
            {
                if (otherMid.Value == mapId && otherEntity != newEntity)
                {
                    var template = poolsService.RentTemplate();
                    charFactory.UpdateFromRuntime(template, otherEntity, World);
                    templates.Add(template);
                }
            });

            // Aluga o array para enviar os dados
            charArray = poolsService.RentArray(templates.Count);
            var snapshotDataSpan = CollectionsMarshal.AsSpan(templates);
            snapshotDataSpan.CopyTo(charArray);
            
            EventBus.Send(new EnterSnapshot(mapId, charId, charArray));
        }
        finally
        {
            // Devolve os CharTemplates individuais para o pool
            foreach (var template in templates)
            {
                poolsService.ReturnTemplate(template);
            }
            
            // Limpa e devolve a lista
            templates.Clear();
            poolsService.ReturnList(templates);
            
            // CORREÇÃO: Devolve o array alugado dentro do finally.
            if(charArray is not null)
                poolsService.ReturnArray(charArray);
        }
    }

    private void SendCharSnapshot(int mapId, int charId, Entity entity)
    {
        var template = poolsService.RentTemplate();
        try
        {
            charFactory.UpdateFromRuntime(template, entity, World);
            EventBus.Send(new CharSnapshot(mapId, charId, template));
        }
        finally
        {
            // CORREÇÃO: Garante que o objeto seja devolvido mesmo em caso de erro.
            poolsService.ReturnTemplate(template);
        }
    }

    private void SendExitSnapshot(int mapId, int charId, Entity entity)
    {
        var template = poolsService.RentTemplate();
        try
        {
            charFactory.UpdateFromRuntime(template, entity, World);
            EventBus.Send(new ExitSnapshot(mapId, charId, template));
        }
        finally
        {
            // CORREÇÃO: Garante que o objeto seja devolvido mesmo em caso de erro.
            poolsService.ReturnTemplate(template);
        }
    }
}

