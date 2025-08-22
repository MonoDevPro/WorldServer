using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public class IntentsDequeueSystem(World world, IntentsEnqueueSystem enqueuer, IEntityIndex indexer, BoundsIndex mapBounds) 
    : BaseSystem<World, float>(world)
{
    private readonly CommandBuffer _cmd = new(initialCapacity: 256);
    private const int MaxPerTick = 1024;

    public override void Update(in float delta)
    {
        // 1) Consumir filas e transformar em comandos no CommandBuffer
        ConsumeMoveIntents();
        ConsumeTeleportIntents();
        ConsumeAttackIntents();

        // 2) Aplicar tudo atomically no World
        _cmd.Playback(World, dispose: true); // dispose = true limpa o buffer para reuso
    }
    
    private void ConsumeMoveIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && enqueuer.MoveQueue.TryDequeue(out var intent))
        {
            if (!indexer.TryGetByCharId(intent.CharId, out var entity))
                continue; // Entidade não encontrada
            
            if (!World.IsAlive(entity))
                continue; // Entidade não existe ou foi destruída
            
            _cmd.Set(entity, intent);

            processed++;
        }
    }

    private void ConsumeTeleportIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && enqueuer.TeleportQueue.TryDequeue(out var intent))
        {
            if (!indexer.TryGetByCharId(intent.CharId, out var entity))
                continue; // Entidade não encontrada
            
            if (!World.IsAlive(entity))
                continue; // Entidade não existe ou foi destruída
            
            _cmd.Set(entity, intent);
            
            processed++;
        }
    }

    private void ConsumeAttackIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && enqueuer.AttackQueue.TryDequeue(out var intent))
        {
            if (!indexer.TryGetByCharId(intent.CharId, out var entity))
                continue; // Entidade não encontrada
            
            if (!World.IsAlive(entity))
                continue; // Entidade não existe ou foi destruída
            
            _cmd.Set(entity, intent);
            processed++;
        }
    }

    public override void Dispose()
    {
        _cmd.Dispose();
        base.Dispose();
    }
}