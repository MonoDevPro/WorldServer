using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.Application.Ports.Persistence;
using Simulation.Domain.Templates;

namespace Simulation.Application.Services;

public class PlayerStagingArea(IBackgroundTaskQueue saveQueue) : IPlayerStagingArea
{
    private readonly ConcurrentQueue<PlayerData> _pendingLogins = new();
    private readonly ConcurrentQueue<int> _pendingLeaves = new();

    public void StageLogin(PlayerData data)
    {
        _pendingLogins.Enqueue(data);
    }

    public bool TryDequeueLogin(out PlayerData? data) 
        => _pendingLogins.TryDequeue(out data);

    public void StageLeave(int charId)
        => _pendingLeaves.Enqueue(charId);

    public bool TryDequeueLeave(out int charId)
    {
        return _pendingLeaves.TryDequeue(out charId);
    }

    public void StageSave(PlayerData data)
    {
        saveQueue.QueueBackgroundWorkItem(async (sp, ct) =>
        {
            var repo = sp.GetRequiredService<IRepositoryAsync<int, PlayerData>>();
            await repo.UpdateAsync(data.CharId, data, ct);
        });
    }
}