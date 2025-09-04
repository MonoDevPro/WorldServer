/*using System.Collections.Concurrent;
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Client.Core;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Client.Systems;

/// <summary>
/// Sistema cliente que manipula snapshots recebidos do servidor e atualiza o mundo local do ECS.
/// Converte snapshots em componentes/entidades ECS.
/// </summary>
public class SnapshotHandlerSystem : BaseSystem<World, float>, ISnapshotHandler
{
    private readonly ILogger<SnapshotHandlerSystem> _logger;
    private readonly CommandBuffer _cmd = new(256);
    
    // Mapeia CharId para Entity no mundo local
    private readonly ConcurrentDictionary<int, Entity> _charIdToEntity = new();

    // Filas thread-safe para snapshots recebidos da rede (evita mexer no CommandBuffer fora do Update)
    private readonly ConcurrentQueue<EnterSnapshot> _enterQueue = new();
    private readonly ConcurrentQueue<CharSnapshot> _charQueue = new();
    private readonly ConcurrentQueue<ExitSnapshot> _exitQueue = new();
    private readonly ConcurrentQueue<MoveSnapshot> _moveQueue = new();
    private readonly ConcurrentQueue<AttackSnapshot> _attackQueue = new();
    private readonly ConcurrentQueue<TeleportSnapshot> _teleportQueue = new();

    public SnapshotHandlerSystem(World world, ILogger<SnapshotHandlerSystem> logger) : base(world)
    {
        _logger = logger;
    }

    public override void Update(in float delta)
    {
        // Consome snapshots enfileirados pela thread de rede e agenda mudanças no CommandBuffer
        while (_enterQueue.TryDequeue(out var e))
        {
            _logger.LogInformation("Processando EnterSnapshot para CharId {CharId} no MapId {MapId}", e.CharId, e.MapId);
            foreach (var template in e.Templates)
            {
                CreateCharacterEntity(template);
            }
        }

        while (_charQueue.TryDequeue(out var cs))
        {
            _logger.LogInformation("Processando CharSnapshot para CharId {CharId}", cs.CharId);
            CreateCharacterEntity(cs.Template);
        }

        while (_exitQueue.TryDequeue(out var ex))
        {
            _logger.LogInformation("Processando ExitSnapshot para CharId {CharId}", ex.CharId);
            if (_charIdToEntity.TryGetValue(ex.CharId, out var entity))
            {
                // Verificação crucial: garante que a entidade ainda está viva antes de destruí-la
                if (World.IsAlive(entity))
                    _cmd.Destroy(entity);
                
                _charIdToEntity.TryRemove(ex.CharId, out _);
            }
        }

        while (_moveQueue.TryDequeue(out var mv))
        {
            _logger.LogTrace("Processando MoveSnapshot para CharId {CharId}: {OldPos} -> {NewPos}",
                mv.CharId, $"({mv.OldPos.X},{mv.OldPos.Y})", $"({mv.NewPos.X},{mv.NewPos.Y})");
            if (_charIdToEntity.TryGetValue(mv.CharId, out var entity))
            {
                // Verificação crucial
                if (World.IsAlive(entity))
                {
                    _cmd.Set(entity, mv.NewPos);
                }
            }
        }

        while (_attackQueue.TryDequeue(out var atk))
        {
            _logger.LogInformation("Processando AttackSnapshot para CharId {CharId}", atk.CharId);
            if (_charIdToEntity.ContainsKey(atk.CharId))
                _logger.LogInformation("Personagem {CharId} executou um ataque", atk.CharId);
        }

        while (_teleportQueue.TryDequeue(out var tp))
        {
            _logger.LogInformation("Processando TeleportSnapshot para CharId {CharId} para ({X},{Y}) no MapId {MapId}",
                tp.CharId, tp.Position.X, tp.Position.Y, tp.MapId);
            if (_charIdToEntity.TryGetValue(tp.CharId, out var entity))
            {
                // Verificação crucial
                if (World.IsAlive(entity))
                {
                    _cmd.Set(entity, tp.Position);
                    _cmd.Set(entity, new MapId(tp.MapId));
                }
            }
        }

        // Aplica todas as mudanças agendadas no CommandBuffer (na thread principal)
        _cmd.Playback(World, dispose: true);
    }

    public void HandleSnapshot(in EnterSnapshot snapshot)
    {
        _enterQueue.Enqueue(snapshot);
    }

    public void HandleSnapshot(in CharSnapshot snapshot)
    {
        _charQueue.Enqueue(snapshot);
    }

    public void HandleSnapshot(in ExitSnapshot snapshot)
    {
        _exitQueue.Enqueue(snapshot);
    }

    public void HandleSnapshot(in MoveSnapshot snapshot)
    {
        _moveQueue.Enqueue(snapshot);
    }

    public void HandleSnapshot(in AttackSnapshot snapshot)
    {
        _attackQueue.Enqueue(snapshot);
    }

    public void HandleSnapshot(in TeleportSnapshot snapshot)
    {
        _teleportQueue.Enqueue(snapshot);
    }

    private void CreateCharacterEntity(CharTemplate template)
    {
        // Se a entidade já existe, atualiza seus componentes
        if (_charIdToEntity.TryGetValue(template.CharId, out var existingEntity))
        {
            UpdateCharacterEntity(existingEntity, template);
            return;
        }

        // Cria nova entidade para o personagem usando a mesma factory do servidor
        var entity = CharFactory.CreateEntity(_cmd, template);

        _charIdToEntity[template.CharId] = entity;
        
        _logger.LogTrace("Entidade criada para CharId {CharId}: {Name} ({Gender}, {Vocation})", 
            template.CharId, template.Name, template.Gender, template.Vocation);
    }

    private void UpdateCharacterEntity(Entity entity, CharTemplate template)
    {
        _cmd.Set(entity, new MapId(template.MapId));
        _cmd.Set(entity, template.Position);
        _cmd.Set(entity, template.Direction);
        _cmd.Set(entity, new MoveStats { Speed = template.MoveSpeed });
        _cmd.Set(entity, new AttackStats { CastTime = template.AttackCastTime, Cooldown = template.AttackCooldown });
        
        _logger.LogTrace("Entidade atualizada para CharId {CharId}", template.CharId);
    }

    /// <summary>
    /// Obtém a entidade associada a um CharId, se existir
    /// </summary>
    public bool TryGetEntity(int charId, out Entity entity) => _charIdToEntity.TryGetValue(charId, out entity);

    /// <summary>
    /// Limpa todas as entidades de personagens
    /// </summary>
    public void ClearAllCharacters()
    {
        foreach (var entity in _charIdToEntity.Values)
        {
            // Adicione esta verificação para prevenir a dupla destruição
            if (World.IsAlive(entity))
                _cmd.Destroy(entity);
        }
        _charIdToEntity.Clear();
        _logger.LogInformation("Todas as entidades de personagens foram removidas");
    }
}*/