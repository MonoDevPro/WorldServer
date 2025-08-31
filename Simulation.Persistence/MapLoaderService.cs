using System.Text.Json;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Map;
using Simulation.Application.Services;
using Simulation.Domain.Templates;

namespace Simulation.Persistence;

/// <summary>
/// Serviço responsável por carregar mapas do disco (ou criar mapa padrão), 
/// converter para MapData e enfileirar no IMapLoaderSystem para aplicação no World.
/// </summary>
public sealed class MapLoaderService : IMapLoaderService
{
    private readonly IMapLoaderSystem _mapLoaderSystem;
    private readonly ILogger<MapLoaderService> _logger;
    private readonly string _mapsDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    private const string MapPrefix = "map";
    private const string MapExtension = ".mapjson";

    public MapLoaderService(
        IMapLoaderSystem mapLoaderSystem,
        ILogger<MapLoaderService> logger,
        string? mapsDirectory = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        _mapLoaderSystem = mapLoaderSystem ?? throw new ArgumentNullException(nameof(mapLoaderSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapsDirectory = mapsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Maps");
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
    }

    private string GetMapFilePath(int mapId) => Path.Combine(_mapsDirectory, $"{MapPrefix}{mapId}{MapExtension}");

    /// <summary>
    /// Synchronous load: reads (or creates) the MapTemplate, converts to MapData and enqueues into IMapLoaderSystem.
    /// Returns true if the map was successfully queued/applied.
    /// </summary>
    public bool LoadMap(int mapId)
    {
        if (mapId <= 0) throw new ArgumentOutOfRangeException(nameof(mapId));

        MapTemplate template;
        try
        {
            template = LoadMapTemplateFromFile(mapId);
        }
        catch (FileNotFoundException)
        {
            _logger.LogInformation("Map {MapId} file not found: creating default map.", mapId);
            template = CreateDefaultMapTemplate(mapId);
            try
            {
                SaveMapTemplateToFile(template);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save generated default map {MapId} to disk.", mapId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load map {MapId}", mapId);
            return false;
        }

        try
        {
            var mapData = MapService.CreateFromTemplate(template);
            _mapLoaderSystem.EnqueueMapData(mapData);
            _logger.LogInformation("Map {MapId} queued for registration.", mapId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert and enqueue map {MapId}", mapId);
            return false;
        }
    }

    /// <summary>
    /// Async version: reads file asynchronously, creates default if missing, enqueues MapData.
    /// </summary>
    public async Task<bool> LoadMapAsync(int mapId, CancellationToken ct = default)
{
    MapTemplate template;
    try
    {
        template = await LoadMapTemplateFromFileAsync(mapId, ct).ConfigureAwait(false);
    }
    catch (FileNotFoundException)
    {
        _logger.LogInformation("Map {MapId} not found; creating default.", mapId);
        template = CreateDefaultMapTemplate(mapId);
        await SaveMapTemplateToFileAsync(template, ct).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load map {MapId}", mapId);
        return false;
    }

    // Validate template dimensions and arrays BEFORE calling CreateFromTemplate
    var width = template.Width;
    var height = template.Height;
    bool invalidDims = width <= 0 || height <= 0;
    var expected = (invalidDims ? 0 : width * height);

    if (invalidDims)
    {
        _logger.LogWarning("MapTemplate for MapId {MapId} has invalid dimensions (w={Width}, h={Height}). Recreating default map.", mapId, width, height);
        template = CreateDefaultMapTemplate(mapId);
        await SaveMapTemplateToFileAsync(template, ct).ConfigureAwait(false);
    }
    else
    {
        // ensure arrays length
        if (template.TilesRowMajor == null || template.TilesRowMajor.Length != expected)
        {
            _logger.LogWarning("TilesRowMajor length mismatch for MapId {MapId} (expected {Expected}, got {Actual}). Filling with fallback floor.", mapId, expected, template.TilesRowMajor?.Length ?? 0);
            template.TilesRowMajor = new TileType[expected];
            Array.Fill(template.TilesRowMajor, TileType.Floor);
        }

        if (template.CollisionRowMajor == null || template.CollisionRowMajor.Length != expected)
        {
            _logger.LogWarning("CollisionRowMajor length mismatch for MapId {MapId} (expected {Expected}, got {Actual}). Filling with zero collision.", mapId, expected, template.CollisionRowMajor?.Length ?? 0);
            template.CollisionRowMajor = new byte[expected];
        }
    }

    try
    {
        var mapData = MapService.CreateFromTemplate(template); // now should be safe
        _mapLoaderSystem.EnqueueMapData(mapData);
        _logger.LogInformation("Map {MapId} queued for registration (async).", mapId);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to convert and enqueue map {MapId}", mapId);
        return false;
    }
}

    /// <summary>
    /// Save the provided template to disk (synchronous).
    /// </summary>
    public void SaveMapTemplateToFile(MapTemplate template)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        Directory.CreateDirectory(_mapsDirectory);
        var path = GetMapFilePath(template.MapId);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(template, _jsonOptions);
        File.WriteAllBytes(path, bytes);
        _logger.LogInformation("Saved map {MapId} to {Path}", template.MapId, path);
    }

    /// <summary>
    /// Save map template asynchronously.
    /// </summary>
    public async Task SaveMapTemplateToFileAsync(MapTemplate template, CancellationToken ct = default)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        Directory.CreateDirectory(_mapsDirectory);
        var path = GetMapFilePath(template.MapId);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(template, _jsonOptions);
        await File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
        _logger.LogInformation("Saved map {MapId} to {Path} (async)", template.MapId, path);
    }

    private MapTemplate LoadMapTemplateFromFile(int mapId)
    {
        var path = GetMapFilePath(mapId);
        if (!File.Exists(path)) throw new FileNotFoundException("Map file not found", path);

        var bytes = File.ReadAllBytes(path);
        var dto = JsonSerializer.Deserialize<MapTemplate>(bytes, _jsonOptions);
        if (dto == null) throw new InvalidDataException($"Failed to deserialize map {path}");
        return dto;
    }

    private async Task<MapTemplate> LoadMapTemplateFromFileAsync(int mapId, CancellationToken ct = default)
    {
        var path = GetMapFilePath(mapId);
        if (!File.Exists(path))
            throw new FileNotFoundException("Map file not found", path);

        var bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize<MapTemplate>(bytes, _jsonOptions);
        if (dto == null) throw new InvalidDataException($"Failed to deserialize map: {path}");
        return dto;
    }

    /// <summary>
    /// Creates a simple default MapTemplate (in-memory) — does not write to disk unless SaveMapTemplateToFile is called.
    /// </summary>
    private MapTemplate CreateDefaultMapTemplate(int mapId, int width = 50, int height = 50, bool borderBlocked = true)
    {
        var dto = new MapTemplate
        {
            MapId = mapId,
            Name = $"Default Map {mapId}",
            Width = width,
            Height = height,
            UsePadded = false,
        };

        var w = dto.Width;
        var h = dto.Height;
        var tiles = new TileType[w * h];
        var coll = new byte[w * h];
        Array.Fill(tiles, TileType.Floor);

        if (borderBlocked)
        {
            for (int x = 0; x < w; x++)
            {
                coll[x] = 1;
                coll[(h - 1) * w + x] = 1;
            }
            for (int y = 0; y < h; y++)
            {
                coll[y * w] = 1;
                coll[y * w + (w - 1)] = 1;
            }
        }

        dto.TilesRowMajor = tiles;
        dto.CollisionRowMajor = coll;
        return dto;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
