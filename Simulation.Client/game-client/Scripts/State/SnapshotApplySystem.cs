using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using GameClient.Scripts.Infrastructure;
using GameClient.Scripts.Network.Packets;
using Simulation.Application.DTOs;
using Simulation.Domain.Components;

namespace GameClient.Scripts.State;

/// <summary>
/// System responsible for applying server snapshots to the client ECS world
/// </summary>
public class SnapshotApplySystem
{
    private readonly World _world;
    private readonly IClientEventBus _eventBus;
    private readonly Dictionary<int, Entity> _playerEntities = new();

    public SnapshotApplySystem(World world, IClientEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;
        
        // Subscribe to snapshot events
        _eventBus.Subscribe<ClientJoinAckSnapshot>(OnJoinAckSnapshot);
        _eventBus.Subscribe<ClientPlayerJoinedSnapshot>(OnPlayerJoinedSnapshot);
        _eventBus.Subscribe<ClientPlayerLeftSnapshot>(OnPlayerLeftSnapshot);
        _eventBus.Subscribe<ClientMoveSnapshot>(OnMoveSnapshot);
        _eventBus.Subscribe<ClientAttackSnapshot>(OnAttackSnapshot);
        _eventBus.Subscribe<ClientTeleportSnapshot>(OnTeleportSnapshot);
    }

    private void OnJoinAckSnapshot(ClientJoinAckSnapshot packet)
    {
        GD.Print($"Applying JoinAckSnapshot: Creating {packet.Others.Count + 1} players");

        // Create local player entity
        var localPlayerEntity = _world.Create();
        _world.Add(localPlayerEntity, new CharId { Value = packet.YourCharId });
        _world.Add(localPlayerEntity, new MapId { Value = packet.MapId });
        _world.Add(localPlayerEntity, new Position { X = 0, Y = 0 }); // Default position, will be updated by server
        _world.Add(localPlayerEntity, new Direction { X = 0, Y = 1 }); // Default direction
        _playerEntities[packet.YourCharId] = localPlayerEntity;

        // Create entities for other players
        foreach (var playerState in packet.Others)
        {
            CreatePlayerEntity(playerState);
        }
    }

    private void OnPlayerJoinedSnapshot(ClientPlayerJoinedSnapshot packet)
    {
        GD.Print($"Applying PlayerJoinedSnapshot: Adding player {packet.NewPlayer.CharId}");
        CreatePlayerEntity(packet.NewPlayer);
    }

    private void OnPlayerLeftSnapshot(ClientPlayerLeftSnapshot packet)
    {
        GD.Print($"Applying PlayerLeftSnapshot: Removing player {packet.LeftPlayer.CharId}");
        
        if (_playerEntities.TryGetValue(packet.LeftPlayer.CharId, out var entity))
        {
            _world.Destroy(entity);
            _playerEntities.Remove(packet.LeftPlayer.CharId);
        }
    }

    private void OnMoveSnapshot(ClientMoveSnapshot packet)
    {
        if (_playerEntities.TryGetValue(packet.CharId, out var entity))
        {
            // Update position component
            ref var position = ref _world.Get<Position>(entity);
            position.X = packet.New.X;
            position.Y = packet.New.Y;
            
            GD.Print($"Updated player {packet.CharId} position to ({packet.New.X}, {packet.New.Y})");
        }
    }

    private void OnAttackSnapshot(ClientAttackSnapshot packet)
    {
        GD.Print($"Player {packet.CharId} attacked");
        // Handle attack visual effects here if needed
    }

    private void OnTeleportSnapshot(ClientTeleportSnapshot packet)
    {
        if (_playerEntities.TryGetValue(packet.CharId, out var entity))
        {
            // Update position and map
            ref var position = ref _world.Get<Position>(entity);
            ref var mapId = ref _world.Get<MapId>(entity);
            
            position.X = packet.Position.X;
            position.Y = packet.Position.Y;
            mapId.Value = packet.MapId;
            
            GD.Print($"Teleported player {packet.CharId} to map {packet.MapId} at ({packet.Position.X}, {packet.Position.Y})");
        }
    }

    private void CreatePlayerEntity(PlayerState playerState)
    {
        if (_playerEntities.ContainsKey(playerState.CharId))
        {
            GD.PrintErr($"Player entity {playerState.CharId} already exists!");
            return;
        }

        var entity = _world.Create();
        _world.Add(entity, new CharId { Value = playerState.CharId });
        _world.Add(entity, new MapId { Value = playerState.MapId });
        _world.Add(entity, new Position { X = playerState.Position.X, Y = playerState.Position.Y });
        _world.Add(entity, new Direction { X = playerState.Direction.X, Y = playerState.Direction.Y });
        _world.Add(entity, new MoveStats { Speed = playerState.MoveSpeed });
        _world.Add(entity, new AttackStats { CastTime = playerState.AttackCastTime, Cooldown = playerState.AttackCooldown });
        
        _playerEntities[playerState.CharId] = entity;
        GD.Print($"Created player entity for CharId {playerState.CharId}");
    }

    public Entity? GetPlayerEntity(int charId)
    {
        return _playerEntities.TryGetValue(charId, out var entity) ? entity : null;
    }

    public IEnumerable<KeyValuePair<int, Entity>> GetAllPlayerEntities()
    {
        return _playerEntities;
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<ClientJoinAckSnapshot>(OnJoinAckSnapshot);
        _eventBus.Unsubscribe<ClientPlayerJoinedSnapshot>(OnPlayerJoinedSnapshot);
        _eventBus.Unsubscribe<ClientPlayerLeftSnapshot>(OnPlayerLeftSnapshot);
        _eventBus.Unsubscribe<ClientMoveSnapshot>(OnMoveSnapshot);
        _eventBus.Unsubscribe<ClientAttackSnapshot>(OnAttackSnapshot);
        _eventBus.Unsubscribe<ClientTeleportSnapshot>(OnTeleportSnapshot);
    }
}