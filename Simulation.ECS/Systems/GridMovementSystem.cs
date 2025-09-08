using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Domain;
using Simulation.ECS.Services;

namespace Simulation.ECS.Systems;

/// <summary>
/// Processa o movimento de entidades em um grid usando o componente MoveAction.
/// </summary>
public sealed partial class GridMovementSystem(World world, MapManagerService mapManager)
    : BaseSystem<World, float>(world)
{
    // Query para encontrar entidades que querem iniciar um novo movimento.
    // Elas têm uma intenção, mas NÃO estão com uma ação de movimento ativa.
    [Query]
    [All<Position, Direction, MoveStats, MoveIntent, MapId>] // <-- Adicione MapId à query
    [None<MoveAction>] // <- Alterado de MovementTimer para MoveAction
    private void StartMovement(in Entity entity, ref Position pos, ref Direction dir, in MoveStats stats, in MoveIntent intent, in MapId mapId)
    {
        dir = intent.Directioon;

        var targetPosition = new Position
        {
            X = pos.X + intent.Directioon.X,
            Y = pos.Y + intent.Directioon.Y
        };

        if (!mapManager.IsTileBlocked(mapId.Value, targetPosition))
        {
            // Adiciona o componente MoveAction para iniciar o movimento.
            World.Add(entity, new MoveAction
            {
                Start = pos, // <- Armazena a posição inicial
                Target = targetPosition,
                Elapsed = 0f, // <- Começa com tempo decorrido zero
                Duration = 1.0f / stats.Speed // <- Calcula a duração total
            });
        }
        
        World.Remove<MoveIntent>(entity);
    }

    // Query para encontrar entidades que já estão no meio de um movimento.
    [Query]
    [All<Position, MoveAction>] // <- Alterado de MovementTimer para MoveAction
    private void ContinueMovement(in Entity entity, ref Position pos, ref MoveAction action, in float deltaTime)
    {
        // 1. Incrementa o tempo decorrido.
        action.Elapsed += deltaTime;

        // 2. Se o tempo decorrido ultrapassou a duração, o movimento terminou.
        if (action.Elapsed >= action.Duration)
        {
            // 3. Garante que a posição lógica seja exatamente a do destino.
            pos = action.Target;

            // 4. Remove a ação de movimento, liberando a entidade para a próxima.
            World.Remove<MoveAction>(entity);
        }
        else
        {
            // Opcional: Interpolação Visual
            // Aqui você pode atualizar um componente de renderização para uma posição visual suave.
            // A posição LÓGICA (o 'pos' do grid) só é atualizada no final.
            // Por exemplo:
            // ref var transform = ref World.Get<VisualTransform>(entity);
            // float t = action.Elapsed / action.Duration; // Percentual do movimento (0.0 a 1.0)
            // transform.VisualPosition = Vector2.Lerp(action.Start, action.Target, t);
        }
    }
}