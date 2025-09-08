using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Simulation.Domain;
using Simulation.ECS.Systems.Index;

namespace Simulation.ECS.Systems.Handlers;

public sealed class PlayerInputCommandSystem(
    World world,
    PlayerIndexSystem playerIndexSystem)
    : BaseSystem<World, float>(world)
{
    // agora usa DoubleBufferedCommandBuffer em vez de CommandBuffer
    private readonly CommandBuffer _buffer = new(initialCapacity: 1024);
    
    public override void Update(in float deltaTime)
    {
        // Aplica o buffer na main-thread (antes de outros sistemas que podem depender dos comandos)
        _buffer.Playback(World, dispose: true);
    }
    
    public void HandleIntent(int charId, in MoveIntent intent)
    {
        // Acesso O(1) e super rápido!
        if (playerIndexSystem.TryGetEntity(charId, out var entity) && World.IsAlive(entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    public void HandleIntent(int charId, in AttackIntent intent)
    {
        if (playerIndexSystem.TryGetEntity(charId, out var entity) && World.IsAlive(entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    public void HandleIntent(int charId, in TeleportIntent intent)
    {
        if (playerIndexSystem.TryGetEntity(charId, out var entity) && World.IsAlive(entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    // Dispose: remover o dispose caso o CommandBuffer seja 'owned' por outro. 
    // DoubleBufferedCommandBuffer é normalmente registrado e gerenciado pelo DI (singleton).
    public override void Dispose()
    {
        _buffer.Dispose();
        base.Dispose();
    }
}