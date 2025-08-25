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
    private readonly CommandBuffer _cmd = new(initialCapacity: 256);
    private readonly ILogger<IntentsDequeueSystem> _logger;
    private readonly IntentsEnqueueSystem _enqueuer;
    private readonly IEntityIndex _indexer;
    private const int MaxPerTick = 1024;

    // O construtor estava incompleto no último arquivo, corrigindo a injeção de dependência.
    public IntentsDequeueSystem(ILogger<IntentsDequeueSystem> logger, World world, IntentsEnqueueSystem enqueuer, IEntityIndex indexer) 
        : base(world)
    {
        _logger = logger;
        _enqueuer = enqueuer;
        _indexer = indexer;
    }

    public override void Update(in float delta)
    {
        ConsumeMoveIntents();
        ConsumeTeleportIntents();
        ConsumeAttackIntents();
        ConsumeEnterGameIntents();
        ConsumeExitGameIntents();

        _cmd.Playback(World, dispose: true);
    }

    private void ConsumeMoveIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && _enqueuer.MoveQueue.TryDequeue(out var intent))
        {
            if (_indexer.TryGetByCharId(intent.CharId, out var entity) && World.IsAlive(entity))
            {
                _cmd.Set(entity, intent);
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
                _cmd.Set(entity, intent);
                processed++;
            }
        }
    }

    private void ConsumeAttackIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && _enqueuer.AttackQueue.TryDequeue(out var intent))
        {
            if (_indexer.TryGetByCharId(intent.AttackerCharId, out var entity) && World.IsAlive(entity))
            {
                _logger.LogInformation("Processando AttackIntent de CharId {CharId}", intent.AttackerCharId);
                _cmd.Set(entity, intent);
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
            
            // CORREÇÃO: Usando o método Create(intent) que cria a entidade E anexa o componente com seus dados.
            
            var entity = _cmd.Create([Component.GetComponentType(typeof(EnterGameIntent))]);
            entity.Add<EnterGameIntent>(in intent);
            processed++;
        }
    }

    private void ConsumeExitGameIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && _enqueuer.ExitGameQueue.TryDequeue(out var intent))
        {
            _logger.LogInformation("Processando ExitGameIntent para CharId {CharId}", intent.CharacterId);

            // CORREÇÃO: O mesmo aqui, garantindo que o CharacterId seja preservado.
            var entity = _cmd.Create([Component.GetComponentType(typeof(ExitGameIntent))]);
            entity.Add<ExitGameIntent>(in intent);
            processed++;
        }
    }

    public override void Dispose()
    {
        _cmd.Dispose();
        base.Dispose();
    }
}