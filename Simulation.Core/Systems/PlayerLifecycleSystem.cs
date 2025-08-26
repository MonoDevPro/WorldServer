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
using Simulation.Core.Factories;

namespace Simulation.Core.Systems;

public sealed partial class PlayerLifecycleSystem : BaseSystem<World, float>
{
    private readonly IEntityIndex _entityIndex;
    private readonly ILogger<PlayerLifecycleSystem> _logger; // Adicionado

    public PlayerLifecycleSystem(World world, IEntityIndex entityIndex, ILogger<PlayerLifecycleSystem> logger) : base(world) // Adicionado
    {
        _entityIndex = entityIndex;
        _logger = logger; // Adicionado
    }
    
    [Query]
    [All<EnterGameIntent>]
    private void OnEnterGame(in Entity entity, in EnterGameIntent intent)
    {
        if (_entityIndex.TryGetByCharId(intent.CharacterId, out _))
        {
            _logger.LogWarning("EnterGameIntent ignorada para o CharId {CharId} que já está no jogo.", intent.CharacterId);
            World.Destroy(entity);
            return;
        }

        var playerEntity = PlayerFactory.Create(
            World, 
            intent.CharacterId, 
            new GameVector2(10, 10)
        );

        _entityIndex.Register(intent.CharacterId, playerEntity);
        _logger.LogInformation("Jogador {CharId} entrou no jogo. Entidade: {EntityId}", intent.CharacterId, playerEntity.Id);

        World.Destroy(entity);
    }

    [Query]
    [All<ExitGameIntent>]
    private void OnExitGame(in Entity entity, in ExitGameIntent intent)
    {
        if (_entityIndex.TryGetByCharId(intent.CharacterId, out var playerEntity))
        {
            _entityIndex.UnregisterByCharId(intent.CharacterId);
            if (World.IsAlive(playerEntity))
            {
                World.Destroy(playerEntity);
            }
            _logger.LogInformation("Jogador {CharId} saiu do jogo.", intent.CharacterId);
        }

        World.Destroy(entity);
    }
}