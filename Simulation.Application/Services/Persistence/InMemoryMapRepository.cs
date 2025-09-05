using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs.Intents;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.Application.Ports.Persistence;
using Simulation.Application.Ports.Persistence.Persistence;
using Simulation.Domain.Templates;

namespace Simulation.Application.Services.Persistence;

public sealed class InMemoryMapRepository(
    IMapIntentHandler mapIntentHandler,
    ILogger<InMemoryMapRepository> logger)
    : DefaultRepository<int, MapTemplate>(enableReverseLookup: false), IMapRepository, IInitializable
{
    /// <summary>
    /// Inicializa o repositório (seed). Chame durante a startup.
    /// </summary>
    public void Initialize()
    {
        try
        {
            logger.LogInformation("MapTemplateRepository: Initialization started.");
            SeedMapTemplates();
            var total = this.GetAll().Count();
            logger.LogInformation("MapTemplateRepository: Initialization completed. Templates seeded: {Count}", total);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MapTemplateRepository: Initialization failed.");
            throw;
        }
        
        // Mandar os mapas pra dentro do ECS.
        
        foreach (var map in this.GetAll())
        {
            try
            {
                mapIntentHandler.HandleIntent(new LoadMapIntent(map.MapId), map);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MapTemplateRepository: Failed to create Map (MapId={MapId}) in ECS.", map.MapId);
            }
        }
        
    }

    private void SeedMapTemplates()
    {
        logger.LogDebug("SeedMapTemplates: Starting seed of map templates.");

        var maps = new List<MapTemplate>();

        for (int id = 0; id < 4; id++)
        {
            maps.Add(new MapTemplate
            {
                MapId = id,
                Name = $"Default Map {id}",
                Width = 30,
                Height = 30,
                UsePadded = false,
                BorderBlocked = true
            });
        }

        foreach (var map in maps)
        {
            int size = map.Width * map.Height;
            map.TilesRowMajor = new TileType[size];
            map.CollisionRowMajor = new byte[size];

            for (int i = 0; i < size; i++)
            {
                map.TilesRowMajor[i] = TileType.Floor;
                map.CollisionRowMajor[i] = 0;
            }
        }

        foreach (var map in maps)
        {
            if (!this.TryGet(map.MapId, out var _))
            {
                this.Add(map.MapId, map);
                logger.LogInformation("SeedMapTemplates: Added MapTemplate to repository (MapId={MapId}).", map.MapId);
            }
            else
            {
                logger.LogDebug("SeedMapTemplates: Repository already contains MapId={MapId}; skipping add.", map.MapId);
            }
        }

        logger.LogDebug("SeedMapTemplates: Finished seeding map templates.");
    }
}