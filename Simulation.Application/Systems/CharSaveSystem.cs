using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons.Pools;
using Simulation.Domain.Components;

namespace Simulation.Application.Systems;

/// <summary>
/// Sincroniza o CharTemplate no repositório quando os dados de uma entidade no ECS são alterados.
/// </summary>
public sealed partial class CharSaveSystem(
    World world,
    IPoolsService pools,
    ILogger<CharSaveSystem> logger)
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<NeedSave>]
    [All<CharId, MapId, Position, Direction, MoveStats, AttackStats>]
    private void SaveDispatcher(in Entity entity, in CharId cid, in MapId mid, in Position pos, in Direction dir, in MoveStats mv, in AttackStats atk)
    {

        var tpl = pools.RentCharSaveTemplate();
        try
        {
            tpl.Populate(cid, mid, pos, dir, mv, atk);
            EventBus.Send(tpl);
            World.Remove<NeedSave>(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar CharSaveTemplate para CharId {CharId}", cid.Value);
        }
        finally
        {
            pools.ReturnCharSaveTemplate(tpl);
        }
    }
}

