using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons.Persistence;
using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Indexers;
using Simulation.Domain.Templates;
using Simulation.Persistence.Commons;

namespace Simulation.Persistence.Map;

public sealed class MapTemplateRepository(
    IMapTemplateIndex mapTemplateIndex,
    IMapIntentHandler mapIntentHandler,
    ILogger<MapTemplateRepository> logger)
    : DefaultRepository<int, MapTemplate>(enableReverseLookup: false), IMapTemplateRepository, IInitializable
{
    /// <summary>
    /// Inicializa o repositório (seed). Chame durante a startup.
    /// </summary>
    public void Initialize()
    {
        try
        {
            logger.LogInformation("MapTemplateRepository: Initialization started.");
            SeedMapTemplates(mapTemplateIndex);
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
                mapIntentHandler.HandleIntent(new LoadMapIntent(map.MapId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MapTemplateRepository: Failed to create Map (MapId={MapId}) in ECS.", map.MapId);
            }
        }
        
    }

    private void SeedMapTemplates(IMapTemplateIndex index)
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
            if (!index.TryGet(map.MapId, out var existing))
            {
                index.Register(map.MapId, map);
                logger.LogInformation("SeedMapTemplates: Registered MapTemplate in IMapTemplateIndex (MapId={MapId}, Name={Name}).", map.MapId, map.Name);
            }
            else
            {
                logger.LogDebug("SeedMapTemplates: IMapTemplateIndex already had MapId={MapId}; skipping register.", map.MapId);
            }

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