using Arch.Core;
using Arch.Core.Extensions;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Components.Attack;
using Simulation.Core.Abstractions.Commons.Components.Char;
using Simulation.Core.Abstractions.Commons.Components.Move;
using Simulation.Core.Abstractions.Commons.Enums;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.Intents.Out;

namespace Simulation.Core.Factories;

public static class PlayerFactory
{
    /// <summary>
    /// Cria uma nova entidade de jogador e um GameSnapshot inicial para o cliente.
    /// </summary>
    /// <returns>Uma tupla contendo a entidade criada e o snapshot do jogo.</returns>
    public static (Entity, GameSnapshot) Create(World world, int characterId, int mapId, GameVector2 initialPosition)
    {
        var entity = world.Create(
            // --- Componentes Adicionados para uma Entidade Completa ---
            new CharId { CharacterId = characterId },
            new CharInfo { Name = $"Player {characterId}", Gender = Gender.Male, Vocation = Vocation.Mage },
            new MapRef { MapId = mapId },
            new TilePosition { Position = initialPosition },
            new Direction { Value = new GameVector2(0, 1) }, // Olhando para baixo
            new MoveSpeed { Value = 1.0f }, // Velocidade padrão 1 tile/segundo
            new AttackSpeed { CastTime = 0.5f, Cooldown = 1.5f }
        );

        // Cria o snapshot DEPOIS que a entidade foi totalmente criada.
        return (entity, CreateGameSnapshot(entity, world));
    }

    /// <summary>
    /// Cria um snapshot do estado atual do jogo, focado no mapa da entidade de referência.
    /// </summary>
    private static GameSnapshot CreateGameSnapshot(Entity newPlayerEntity, World world)
    {
        // 1. Descobre em que mapa a nova entidade está.
        var mapId = newPlayerEntity.Get<MapRef>().MapId;
        var charId = newPlayerEntity.Get<CharId>().CharacterId;
        
        var characterSnapshots = new List<CharSnapshot>();

        // 2. Cria uma query para encontrar todas as entidades que são personagens no mesmo mapa.
        var query = new QueryDescription()
            .WithAll<CharId, MapRef>();

        // 3. Itera sobre todas as entidades que correspondem à query.
        world.Query(in query, (Entity entity, ref MapRef mapRef) =>
        {
            // Adiciona apenas as entidades que estão no mesmo mapa.
            if (mapRef.MapId == mapId)
            {
                characterSnapshots.Add(CreateCharacterSnapshot(entity, world));
            }
        });

        // 4. Retorna o snapshot final.
        return new GameSnapshot(mapId, charId, characterSnapshots.ToArray());
    }

    /// <summary>
    /// Cria um snapshot do estado de uma única entidade de personagem.
    /// </summary>
    private static CharSnapshot CreateCharacterSnapshot(Entity entity, World world)
    {
        // Coleta todos os componentes necessários da entidade.
        var mapRef = world.Get<MapRef>(entity);
        var id = world.Get<CharId>(entity);
        var info = world.Get<CharInfo>(entity);
        var position = world.Get<TilePosition>(entity);
        var direction = world.Get<Direction>(entity);
        var moveSpeed = world.Get<MoveSpeed>(entity);
        var attackSpeed = world.Get<AttackSpeed>(entity);

        return new CharSnapshot(mapRef, id, info, position, direction, moveSpeed, attackSpeed);
    }

    /// <summary>
    /// Cria um snapshot simples para notificar a saída de um personagem.
    /// </summary>
    public static CharExitSnapshot ExitSnapshot(int characterId) =>
        new() { CharId = characterId };
}