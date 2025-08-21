using Arch.Core;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.In.Factories;
using Simulation.Core.Commons;
using Simulation.Core.Commons.Enums;
using Simulation.Core.Components;

namespace Simulation.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly World _world;
    private readonly IEntityFactory _entities;

    public Worker(ILogger<Worker> logger, World world, IEntityFactory entities)
    {
        _logger = logger;
        _world = world;
        _entities = entities;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Seed: cria mapa com limites e um personagem com id 1
        _world.Create(new Bounds { MinX = -100, MinY = -100, MaxX = 100, MaxY = 100 }, new MapRef { MapId = 1 });
        _entities.CreateEntity(_world, 1, new CharacterData
        {
            Id = 1,
            Name = "Hero",
            Vocation = Vocation.Mage,
            Gender = Gender.Male,
            Direction = GameVector2.Zero,
            Position = new GameVector2(0, 0),
            Speed = 5f
        });

        _logger.LogInformation("Seeded map 1 and character 1 at (0,0)");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}