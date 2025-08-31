using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Client.Core;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Commons;

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
    private readonly Dictionary<int, Entity> _charIdToEntity = new();

    public SnapshotHandlerSystem(World world, ILogger<SnapshotHandlerSystem> logger) : base(world)
    {
        _logger = logger;
    }

    public override void Update(in float delta)
    {
        // Aplica todas as mudanças agendadas no CommandBuffer
        _cmd.Playback(World, dispose: true);
    }

    public void HandleSnapshot(in EnterSnapshot snapshot)
    {
        _logger.LogInformation("Processando EnterSnapshot para CharId {CharId} no MapId {MapId}", 
            snapshot.charId, snapshot.mapId);
        
        // Cria entidades para todos os personagens no mapa
        foreach (var template in snapshot.templates)
        {
            CreateCharacterEntity(template);
        }
    }

    public void HandleSnapshot(in CharSnapshot snapshot)
    {
        _logger.LogTrace("Processando CharSnapshot para CharId {CharId}", snapshot.CharId);
        CreateCharacterEntity(snapshot.Template);
    }

    public void HandleSnapshot(in ExitSnapshot snapshot)
    {
        _logger.LogInformation("Processando ExitSnapshot para CharId {CharId}", snapshot.CharId);
        
        if (_charIdToEntity.TryGetValue(snapshot.CharId, out var entity))
        {
            _cmd.Destroy(entity);
            _charIdToEntity.Remove(snapshot.CharId);
        }
    }

    public void HandleSnapshot(in MoveSnapshot snapshot)
    {
        _logger.LogTrace("Processando MoveSnapshot para CharId {CharId}: {OldPos} -> {NewPos}", 
            snapshot.CharId, $"({snapshot.OldPosition.X},{snapshot.OldPosition.Y})", 
            $"({snapshot.NewPosition.X},{snapshot.NewPosition.Y})");
        
        if (_charIdToEntity.TryGetValue(snapshot.CharId, out var entity))
        {
            _cmd.Set(entity, snapshot.NewPosition);
        }
    }

    public void HandleSnapshot(in AttackSnapshot snapshot)
    {
        _logger.LogInformation("Processando AttackSnapshot para CharId {CharId}", snapshot.CharId);
        
        // Por enquanto, apenas logamos. Podemos adicionar componentes visuais posteriormente.
        if (_charIdToEntity.ContainsKey(snapshot.CharId))
        {
            _logger.LogInformation("Personagem {CharId} executou um ataque", snapshot.CharId);
        }
    }

    public void HandleSnapshot(in TeleportSnapshot snapshot)
    {
        _logger.LogInformation("Processando TeleportSnapshot para CharId {CharId} para ({X},{Y}) no MapId {MapId}", 
            snapshot.CharId, snapshot.Position.X, snapshot.Position.Y, snapshot.MapId);
        
        if (_charIdToEntity.TryGetValue(snapshot.CharId, out var entity))
        {
            _cmd.Set(entity, snapshot.Position);
            _cmd.Set(entity, new MapId(snapshot.MapId));
        }
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
            _cmd.Destroy(entity);
        }
        _charIdToEntity.Clear();
        _logger.LogInformation("Todas as entidades de personagens foram removidas");
    }
}