using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Loop;
using Simulation.Application.Ports.Persistence;
using Simulation.Application.Ports.Persistence.Persistence;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.Services.Persistence;

/// <summary>
/// Implementação em memória do ICharTemplateRepository, thread-safe.
/// <para>
/// Opcionalmente aceita um cloneFunc para clonar templates ao gravar/ler.
/// Use cloneFunc se CharTemplate for uma class mutável e você quiser evitar races.
/// </para>
/// </summary>
public sealed class InMemoryPlayerRepository : DefaultRepository<int, PlayerTemplate>, IPlayerRepository, IInitializable
{
    private readonly ILogger<InMemoryPlayerRepository> _logger;

    public InMemoryPlayerRepository(
        ILogger<InMemoryPlayerRepository> logger) : base(enableReverseLookup: false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("CharTemplateRepository: Initialization started.");
            SeedChars();
            var total = this.GetAll().Count();
            _logger.LogInformation("CharTemplateRepository: Initialization completed. Templates seeded: {Count}", total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CharTemplateRepository: Initialization failed.");
            throw;
        }
        return Task.CompletedTask;
    }

    private void SeedChars()
    {
        _logger.LogDebug("SeedChars: Starting seed of character templates.");

        var chars = new List<PlayerTemplate>
        {
            new() { CharId = 1, Name = "Filipe", MapId = 1, Position = new Position{ X = 5, Y = 5 }, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { CharId = 2, Name = "Rodorfo", MapId = 1, Position = new Position{ X = 8, Y = 8 }, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
        };

        foreach (var ch in chars)
        {
            if (!this.TryGet(ch.CharId, out var _))
            {
                this.Add(ch.CharId, ch);
                _logger.LogInformation("SeedChars: Added CharTemplate to repository (CharId={CharId}).", ch.CharId);
            }
            else
            {
                _logger.LogDebug("SeedChars: Repository already contains CharId={CharId}; skipping add.", ch.CharId);
            }
        }

        _logger.LogDebug("SeedChars: Finished seeding character templates.");
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}