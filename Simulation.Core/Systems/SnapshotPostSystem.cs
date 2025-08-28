using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Systems;

public partial class SnapshotPostSystem(World world, ILogger<SnapshotPostSystem> logger)
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<EnterSnapshot>]
    private void ProcessGameSnapshot(in Entity entity, in EnterSnapshot snapshot)
    {
        try
        {
            EventBus.Send(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish EnterSnapshot for CharId {CharId}", snapshot.currentCharId);
        }
        finally
        {
            // EnterSnapshot foi um componente de "intent" — destruímos a entidade que o continha
            World.Destroy(entity);
        }
    }

    [Query]
    [All<ExitSnapshot>]
    private void ProcessCharExitSnapshot(in Entity entity, in ExitSnapshot snapshot)
    {
        try
        {
            EventBus.Send(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish ExitSnapshot for CharId {CharId}", snapshot.CharId);
        }
        finally
        {
            World.Destroy(entity);
        }
    }

    // MoveSnapshot pode estar preso na mesma entidade do jogador (component) ou em uma entidade snapshot separada.
    // Aqui assumimos que MoveSnapshot é componente anexado ao jogador.
    [Query]
    [All<MoveSnapshot, MapId, Position>]
    private void ProcessMoveSnapshot(in Entity entity, in MoveSnapshot snapshot, in MapId mapId, in Position pos)
    {
        try
        {
            EventBus.Send(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish MoveSnapshot for CharId {CharId}", snapshot.CharId);
        }
        finally
        {
            // Retira o componente MoveSnapshot (mantendo a entidade do jogador viva)
            if (World.Has<MoveSnapshot>(entity))
                World.Remove<MoveSnapshot>(entity);
        }
    }

    [Query]
    [All<AttackSnapshot, MapId, Position>]
    private void ProcessAttackSnapshot(in Entity entity, in AttackSnapshot snapshot, in MapId mapId, in Position pos)
    {
        try
        {
            EventBus.Send(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish AttackSnapshot for CharId {CharId}", snapshot.CharId);
        }
        finally
        {
            if (World.Has<AttackSnapshot>(entity))
                World.Remove<AttackSnapshot>(entity);
        }
    }
}
