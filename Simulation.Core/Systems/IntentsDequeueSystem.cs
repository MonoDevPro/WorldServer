using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Core.Systems;

public class IntentsDequeueSystem : BaseSystem<World, float>
{
    private readonly ILogger<IntentsDequeueSystem> _logger;
    private readonly IntentsEnqueueSystem _enqueuer;
    private readonly IEntityIndex _indexer;
    private const int MaxPerTick = 2;

    // O construtor estava incompleto no último arquivo, corrigindo a injeção de dependência.
    public IntentsDequeueSystem(ILogger<IntentsDequeueSystem> logger, 
        World world, IntentsEnqueueSystem enqueuer, IEntityIndex indexer) 
        : base(world)
    {
        _logger = logger;
        _enqueuer = enqueuer;
        _indexer = indexer;
    }

    public override void Update(in float delta)
    {
        ConsumeEnterGameIntents();
        ConsumeExitGameIntents();
        ConsumeMoveIntents();
        ConsumeTeleportIntents();
        ConsumeAttackIntents();
    }

    private void ConsumeMoveIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && _enqueuer.MoveQueue.TryDequeue(out var intent))
        {
            if (_indexer.TryGetByCharId(intent.CharId, out var entity) && World.IsAlive(entity))
            {
                World.Set(entity, intent);
                processed++;
            }
        }
    }

    private void ConsumeTeleportIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && _enqueuer.TeleportQueue.TryDequeue(out var intent))
        {
            if (_indexer.TryGetByCharId(intent.CharId, out var entity) && World.IsAlive(entity))
            {
                World.Set(entity, intent);
                processed++;
            }
        }
    }

    private void ConsumeAttackIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && _enqueuer.AttackQueue.TryDequeue(out var intent))
        {
            _logger.LogInformation("Attack Queue Size: {QueueSize}", _enqueuer.AttackQueue.Count);
            
            if (_indexer.TryGetByCharId(intent.AttackerCharId, out var entity) && World.IsAlive(entity))
            {
                _logger.LogInformation("Processando AttackIntent de CharId {CharId}", intent.AttackerCharId);
                World.Set(entity, intent);
                processed++;
            }
        }
    }

    private void ConsumeEnterGameIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && _enqueuer.EnterGameQueue.TryDequeue(out var intent))
        {
            _logger.LogInformation("Processando EnterGameIntent para CharId {CharId}", intent.CharacterId);
            
            World.Create(intent);
            processed++;
        }
    }

    private void ConsumeExitGameIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && _enqueuer.ExitGameQueue.TryDequeue(out var intent))
        {
            _logger.LogInformation("Processando ExitGameIntent para CharId {CharId}", intent.CharacterId);

            World.Create(intent);
            processed++;
        }
    }
}