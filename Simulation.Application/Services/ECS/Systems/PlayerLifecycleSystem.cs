using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.DTOs.Intents;
using Simulation.Application.DTOs.Snapshots;
using Simulation.Application.Ports.ECS;
using Simulation.Application.Ports.ECS.Utils;
using Simulation.Application.Ports.ECS.Utils.Indexers;
using Simulation.Application.Ports.Persistence;
using Simulation.Application.Services.ECS.Handlers;

namespace Simulation.Application.Services.ECS.Systems;

public sealed partial class PlayerLifecycleSystem(
    World world,
    IStateSnapshotBuilder snapshotBuilder,
    IMapEntityProvider mapProvider,
    IPlayerIndex index,
    IPlayerRepository playerRepository,
    IFactoryHelper<PlayerStateDto> factory,
    IntentForwarding? intentForwarding,
    ILogger<PlayerLifecycleSystem> logger) : BaseSystem<World, float>(world: world)
{
    // Nota: Query attributes devem corresponder exatamente aos componentes que o método recebe.
    [Query]
    [All<EnterIntent>]
    private void OnEnterRequest(in Entity commandEntity, ref EnterIntent enter)
    {
        PlayerStateDto? dto = null;
        try
        {
            // 3. Carregar o PlayerTemplate a partir do repositório
            if (!playerRepository.TryGet(enter.CharId, out var template) || template == null)
            {
                logger.LogError("CharId {CharId} tentou entrar mas não foi encontrado no repositório.", enter.CharId);
                World.Destroy(commandEntity);
                intentForwarding?.TryRemoveReservation(enter.CharId);
                return;
            }
            
            // 4. Criar o DTO a partir do template carregado (o servidor é a fonte da verdade)
            dto = PlayerStateDto.CreateFromTemplate(template);
            
            // Cria a entidade final no World
            var archetype = factory.GetArchetype();
            var player = World.Create(archetype);

            // Aplica componentes (factory helper aplica campos do DTO/template)
            factory.ApplyTo(World, player, dto);

            // Registrar, enviar snapshots e liberar reserva são feitos em FinalizeJoin
            FinalizeJoin(in enter, in dto, player);

            // Destrói entidade-comando (política escolhida: destruir)
            World.Destroy(commandEntity);
        }
        catch (Exception ex)
        {
            var failingChar = dto?.CharId ?? enter.CharId;
            logger.LogError(ex, "Error creating player for CharId {CharId}", failingChar);
            // garantir que a reserva seja liberada em caso de falha
            intentForwarding?.TryRemoveReservation(failingChar);
            // limpar comando para não tentar de novo
            try { World.Destroy(commandEntity); } catch { /* swallow */ }
        }
    }

    [Query]
    [All<ExitIntent>]
    private void OnExitRequest(in Entity e, in ExitIntent exit)
    {
        try
        {
            // Se encontramos a entidade-jogador no índice, processamos a saída
            if (index.TryGet(exit.CharId, out var playerEntity))
            {
                // FinalizeLeft fará o unregister e notificações
                FinalizeLeft(exit.CharId, playerEntity);

                // Destrói a entidade-jogador
                if (World.IsAlive(playerEntity))
                    World.Destroy(playerEntity);
            }
            else
            {
                // possivelmente trata-se de um comando de saída (entidade-comando)
                if (!World.IsAlive(e)) return;

                // Remove o componente exit (evita loop) e destrói o comando
                World.Remove<ExitIntent>(e);
                World.Destroy(e);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling ExitIntent for CharId {CharId}", exit.CharId);
        }
    }

    // Finaliza o fluxo de "join": registra, constrói snapshots, envia mensagens e libera reserva.
    private void FinalizeJoin(in EnterIntent enter, in PlayerStateDto dto, Entity playerEntity)
    {
        // 1) Registrar no índice (deve ocorrer antes de consultar outros jogadores)
        index.Register(dto.CharId, playerEntity);

        // 2) construir snapshot do novo player
        var newPlayer = snapshotBuilder.BuildCharState(World, playerEntity);

        // 3) obter outros players no mapa (exclui o próprio)
        var othersEntities = mapProvider.GetEntitiesInMap(World, dto.MapId);
        var others = new List<PlayerStateDto>();
        foreach (var e in othersEntities)
        {
            if (e.Id == playerEntity.Id) continue;
            // converter para PlayerStateDto - assumo que BuildCharState retorna PlayerStateDto
            others.Add(snapshotBuilder.BuildCharState(World, e));
        }

        // 4) montar e enviar JoinAck para quem entrou
        // Necessário que enter.SessionId (ou dto.SessionId) exista para indicar destino
        var joinAck = new JoinAckDto(dto.CharId, playerEntity.Id, dto.MapId, others);
        // Encaminha evento para a camada de rede (handler do EventBus deve saber enviar apenas ao session correto).
        // Recomendo que o JoinAck contenha a SessionId ou que o EventBus handler mapeie CharId->SessionId.
        EventBus.Send(joinAck); 

        // 5) notificar os demais do mapa (pode ser broadcast filtrado pelo handler do EventBus)
        var joinedMsg = new PlayerJoinedDto(newPlayer);
        EventBus.Send(joinedMsg);

        // 6) liberar reserva (sucesso)
        intentForwarding?.TryRemoveReservation(dto.CharId);
    }

    // Finaliza o fluxo de saída: unregister, notifica demais.
    private void FinalizeLeft(int charId, Entity playerEntity)
    {
        // 1) remover do índice (main-thread)
        index.Unregister(charId);

        // 2) construir snapshot do jogador que saiu
        var playerleft = snapshotBuilder.BuildCharState(World, playerEntity);

        // 3) notificar os demais do mapa
        var leftMsg = new PlayerLeftDto(playerleft);
        EventBus.Send(leftMsg);
    }
}
