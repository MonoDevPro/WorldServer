using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging; // Adicionado
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Components.Attack;
using Simulation.Core.Abstractions.Commons.Components.Char;
using Simulation.Core.Abstractions.Commons.Components.Move;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Factories;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed partial class PlayerLifecycleSystem : BaseSystem<World, float>
{
    private readonly IEntityIndex _entityIndex;
    private readonly ISpatialIndex _grid;
    private readonly ILogger<PlayerLifecycleSystem> _logger; // Adicionado

    public PlayerLifecycleSystem(World world, IEntityIndex entityIndex, ISpatialIndex grid, ILogger<PlayerLifecycleSystem> logger) : base(world) // Adicionado
    {
        _entityIndex = entityIndex;
        _grid = grid;
        _logger = logger; // Adicionado
    }
    
    [Query]
    [All<EnterGameIntent>]
    private void OnEnterGame(in Entity entity, in EnterGameIntent intent)
    {
        var charId = intent.CharacterId;
        if (_entityIndex.TryGetByCharId(charId, out _))
        {
            _logger.LogWarning("EnterGameIntent ignorada para o CharId {CharId} que já está no jogo.", charId);
            World.Destroy(entity);
            return;
        }
        
        var mapId = 1; // Mapa padrão
        var pos = new GameVector2(10, 10); // Posição inicial padrão

        var (e, snapshot) = PlayerFactory.Create(World, charId, mapId, pos);

        _entityIndex.Register(charId, e);
        _grid.Register(e.Id, mapId, pos);
        
        _logger.LogInformation("Jogador {CharId} entrou no jogo. Entidade: {EntityId}", charId, e.Id);
        World.Destroy(entity);
        
        // Aqui você pode enfileirar o CharSnapshot para ser enviado ao cliente
        World.Create(snapshot);
        
        // Você também pode notificar outros jogadores no mesmo mapa sobre a entrada do novo jogador
        CharSnapshot? charSnapshot = snapshot.AllEntities.FirstOrDefault(cs => cs.CharId.CharacterId == charId);

        if (charSnapshot is null)
        {
            _logger.LogError("CharSnapshot para o CharId {CharId} não encontrado no GameSnapshot.", charId);
            return;
        }
        
        World.Create(charSnapshot);
    }

    [Query]
    [All<ExitGameIntent>]
    private void OnExitGame(in Entity entity, in ExitGameIntent intent)
    {
        if (_entityIndex.TryGetByCharId(intent.CharacterId, out var playerEntity))
        {
            _entityIndex.UnregisterByCharId(intent.CharacterId);
            _grid.Unregister(playerEntity.Id, World.Get<MapRef>(playerEntity).MapId);
            if (World.IsAlive(playerEntity))
            {
                World.Destroy(playerEntity);
            }
            _logger.LogInformation("Jogador {CharId} saiu do jogo.", intent.CharacterId);
        }
        
        // Aqui você pode enfileirar uma notificação de saída para ser enviada ao cliente
        var snapshot = PlayerFactory.ExitSnapshot(intent.CharacterId);
        World.Create(snapshot);

        World.Destroy(entity);
    }
}