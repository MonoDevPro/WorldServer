using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Factories;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed partial class PlayerLifecycleSystem(
    World world,
    IEntityIndex entityIndex,
    ISpatialIndex grid,
    ILogger<PlayerLifecycleSystem> logger)
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<EnterGameIntent>]
    private void OnEnterGame(in Entity entity, in EnterGameIntent intent)
    {
        var charId = intent.CharacterId;
        if (entityIndex.TryGetByCharId(charId, out _))
        {
            logger.LogWarning("EnterGameIntent ignorada para o CharId {CharId} que já está no jogo.", charId);
            World.Destroy(entity);
            return;
        }
        
        var mapId = 1; // Mapa padrão
        var pos = new GameVector2(10, 10); // Posição inicial padrão

        var (e, snapshot) = PlayerFactory.Create(World, charId, mapId, pos);

        entityIndex.Register(charId, e);
        grid.Register(e.Id, mapId, pos);
        
        logger.LogInformation("Jogador {CharId} entrou no jogo. Entidade: {EntityId}", charId, e.Id);
        World.Destroy(entity);
        
        // Aqui você pode enfileirar o GameSnapshot para ser enviado ao cliente
        World.Create(snapshot);
    }

    [Query]
    [All<ExitGameIntent>]
    private void OnExitGame(in Entity entity, in ExitGameIntent intent)
    {
        if (entityIndex.TryGetByCharId(intent.CharacterId, out var playerEntity))
        {
            entityIndex.UnregisterByCharId(intent.CharacterId);
            grid.Unregister(playerEntity.Id, World.Get<MapRef>(playerEntity).MapId);
            if (World.IsAlive(playerEntity))
            {
                World.Destroy(playerEntity);
            }
            logger.LogInformation("Jogador {CharId} saiu do jogo.", intent.CharacterId);
        }
        
        // Aqui você pode enfileirar uma notificação de saída para ser enviada ao cliente
        var snapshot = PlayerFactory.ExitSnapshot(intent.CharacterId);
        World.Create(snapshot);

        World.Destroy(entity);
    }
}