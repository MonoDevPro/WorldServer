using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Core.Systems;

public class IntentsDequeueSystem(ILogger<IntentsDequeueSystem> logger, World world, IntentsEnqueueSystem enqueuer, IEntityIndex indexer) 
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
        ConsumeEnterGameIntents(); // Adicionado
        ConsumeExitGameIntents();  // Adicionado

        // 2) Aplicar tudo atomicamente no World
        _cmd.Playback(World, dispose: true);
    }
    
    // Métodos existentes (ConsumeMoveIntents, etc.) permanecem os mesmos...
    
    private void ConsumeMoveIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && enqueuer.MoveQueue.TryDequeue(out var intent))
        {
            if (!indexer.TryGetByCharId(intent.CharId, out var entity))
                continue; 
            
            if (!World.IsAlive(entity))
                continue; 
            
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
                continue; 
            
            if (!World.IsAlive(entity))
                continue; 
            
            _cmd.Set(entity, intent);
            
            processed++;
        }
    }

    private void ConsumeAttackIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && enqueuer.AttackQueue.TryDequeue(out var intent))
        {
            logger.LogInformation("Processando AttackIntent de CharId {CharId}", intent.AttackerCharId);
            
            if (!indexer.TryGetByCharId(intent.AttackerCharId, out var entity))
                continue; 
            
            if (!World.IsAlive(entity))
                continue; 
            
            _cmd.Set(entity, intent);
            processed++;
        }
    }

    // Novos métodos para processar as intenções de ciclo de vida.
    private void ConsumeEnterGameIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && enqueuer.EnterGameQueue.TryDequeue(out var intent))
        {
            logger.LogInformation("Processando EnterGameIntent para CharId {CharId}", intent.CharacterId);
            
            // Cria uma nova entidade vazia e anexa a intenção.
            // O PlayerLifecycleSystem irá processar esta entidade.
            var entity = _cmd.Create([Component<EnterGameIntent>.ComponentType]);
            _cmd.Set<EnterGameIntent>(entity, intent);
            processed++;
        }
    }

    private void ConsumeExitGameIntents()
    {
        int processed = 0;
        while (processed < MaxPerTick && enqueuer.ExitGameQueue.TryDequeue(out var intent))
        {
            logger.LogInformation("Processando ExitGameIntent para CharId {CharId}", intent.CharacterId);
            
            // Semelhante ao EnterGame, cria uma entidade de comando.
            _cmd.Create([Component<ExitGameIntent>.ComponentType]);
            processed++;
        }
    }

    public override void Dispose()
    {
        _cmd.Dispose();
        base.Dispose();
    }
}