using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.Application.Ports.Pool;
using Simulation.Domain;
using Simulation.Domain.Templates;
using Simulation.ECS.Services;

namespace Simulation.ECS.Systems;

// Classe refatorada para usar apenas PlayerData
public sealed class PlayerLifecycleSystem(
    World world,
    IPlayerStagingArea stagingArea,
    MapManagerService mapManagerService,
    ILogger<PlayerLifecycleSystem> logger,
    IPool<PlayerData>? dataPool = null, // <- Pool de PlayerData
    IPool<List<Entity>>? entityPool = null,
    IPool<List<PlayerData>>? listPool = null) // <- Pool de List<PlayerData>
    : BaseSystem<World, float>(world)
{
    private readonly Dictionary<int, Entity> _playersByCharId = new();
    
    private static readonly QueryDescription CharQuery = new QueryDescription().WithAll<CharId, MapId>();
    
    private static readonly ComponentType[] ArchetypeComponents =
    [
        Component<CharId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<PlayerData>.ComponentType, // Adicione componentes que representam dados do PlayerData se necessário
        Component<Position>.ComponentType,
        Component<Direction>.ComponentType,
        Component<MoveStats>.ComponentType,
        Component<AttackStats>.ComponentType,
        Component<Health>.ComponentType,
        
    ];
    public void ApplyTo(Entity e, PlayerData data)
    {
        World.Set(e,
            new CharId { Value = data.CharId },
            new MapId { Value = data.MapId },
            data, // Armazena o DTO inteiro como um componente
            new Position { X = data.PosX, Y = data.PosY},
            new Direction { X = data.DirX, Y = data.DirY},
            new AttackStats { CastTime = data.AttackCastTime, Cooldown = data.AttackCooldown, Damage = data.AttackDamage, AttackRange = data.AttackRange },
            new MoveStats { Speed = data.MoveSpeed },
            new Health { Current = data.HealthCurrent, Max = data.HealthMax }
        );
    }
    
    public override void Update(in float dt)
    {
        // Processa jogadores entrando
        while (stagingArea.TryDequeueLogin(out var data) && data != null)
            ProcessJoin(data);
        
        // Processa jogadores saindo
        while (stagingArea.TryDequeueLeave(out var charId))
            ProcessLeave(charId);
    }
    
    private async void ProcessJoin(PlayerData data)
    {
        try
        {
            if (_playersByCharId.ContainsKey(data.CharId))
            {
                logger.LogWarning("CharId {CharId} já está no jogo. Ignorando Join.", data.CharId);
                return;
            }
        
            var others = listPool?.Rent() ?? [];
            var mapEntitiesList = entityPool?.Rent() ?? [];
            try
            {
                await mapManagerService.LoadMapAsync(data.MapId);
            
                var entity = World.Create(ArchetypeComponents);
                ApplyTo(entity, data);
            
                _playersByCharId[data.CharId] = entity;
            
                // Obtém outros players no mapa
                var othersEntities = GetEntitiesInMap(data.MapId, mapEntitiesList);
                foreach (var e in othersEntities)
                {
                    if (e.Id == entity.Id) continue;
                
                    others.Add(BuildPlayerData(e));
                }
            
                // Envia JoinAck para quem entrou
                EventBus.Send(new JoinAckSnapshot { MapId = data.MapId, YourCharId = data.CharId, Others = others });

                // Notifica os demais do mapa
                EventBus.Send(new PlayerJoinedSnapshot { NewPlayer = data });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao criar jogador para CharId {CharId}", data.CharId);
            }
            finally
            {
                listPool?.Return(others);
                entityPool?.Return(mapEntitiesList);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro inesperado ao processar Join para CharId {CharId}", data.CharId);
        }
    }
    
    private void ProcessLeave(int charId)
    {
        if (!_playersByCharId.TryGetValue(charId, out var playerEntity))
        {
            logger.LogWarning("CharId {CharId} não encontrado ao processar Leave.", charId);
            return;
        }
        
        try
        {
            // Enfileira o estado final do jogador para ser salvo no banco
            SavePlayerState(playerEntity);
            
            _playersByCharId.Remove(charId);
            
            // Constrói o DTO do jogador que saiu para notificar os outros
            var data = BuildPlayerData(playerEntity);
            EventBus.Send(new PlayerLeftSnapshot { LeftPlayer = data });
            
            if (World.IsAlive(playerEntity))
                World.Destroy(playerEntity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar saída do CharId {CharId}", charId);
        }
    }
    
    private void SavePlayerState(Entity playerEntity)
    {
        var data = BuildPlayerData(playerEntity);
        stagingArea.StageSave(data); // A StagingArea agora recebe PlayerData
    }
    
    private IEnumerable<Entity> GetEntitiesInMap(int mapId, List<Entity>? reuse = null)
    {
        reuse ??= [];

        World.Query(CharQuery, (ref Entity e, ref CharId cid, ref MapId mid) =>
        {
            if (mid.Value == mapId)
                reuse.Add(e);
        });

        return reuse;
    }

    public PlayerData BuildPlayerData(Entity e)
    {
        var pData = World.Get<PlayerData>(e);
        
        // Em vez de pegar cada componente individualmente, podemos pegar o componente PlayerData
        // que armazena o estado completo, tornando a construção de snapshots mais rápida.
        ref var mapId = ref World.Get<MapId>(e);
        ref var position = ref World.Get<Position>(e);
        ref var direction = ref World.Get<Direction>(e);
        ref var attackStats = ref World.Get<AttackStats>(e);
        ref var moveStats = ref World.Get<MoveStats>(e);
        ref var health = ref World.Get<Health>(e);
        
        pData.MapId = mapId.Value;
        pData.PosX = position.X;
        pData.PosY = position.Y;
        pData.DirX = direction.X;
        pData.DirY = direction.Y;
        pData.AttackCastTime = attackStats.CastTime;
        pData.AttackCooldown = attackStats.Cooldown;
        pData.AttackDamage = attackStats.Damage;
        pData.AttackRange = attackStats.AttackRange;
        pData.MoveSpeed = moveStats.Speed;
        pData.HealthCurrent = health.Current;
        pData.HealthMax = health.Max;
        return pData;
    }
}