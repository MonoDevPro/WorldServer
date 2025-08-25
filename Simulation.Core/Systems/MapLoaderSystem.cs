using System.Text.Json;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging; // Adicionado
using Simulation.Core.Abstractions.Commons.Components.Map;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed partial class MapLoaderSystem : BaseSystem<World, float>
{
    private readonly ILogger<MapLoaderSystem> _logger; // Adicionado
    private readonly string _mapsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Maps");
    private const string MapPrefix = "map";
    private const string MapExtension = ".mapjson";

    public MapLoaderSystem(World world, ILogger<MapLoaderSystem> logger) : base(world) // Adicionado
    {
        _logger = logger; // Adicionado
    }
    
    private string GetMapFilePath(int mapId) => Path.Combine(_mapsDirectory, $"{MapPrefix}{mapId}{MapExtension}");

    [Query]
    [All<WantsToLoadMap>]
    private void OnLoadMapRequest(in Entity cmdEntity, ref WantsToLoadMap cmd)
    {
        if (IsMapLoaded(cmd.MapId))
        {
            World.Destroy(cmdEntity);
            return;
        }
        
        MapDto? mapDto;
        try
        {
            mapDto = LoadMapDtoFromFile(cmd.MapId);
        }
        catch (FileNotFoundException)
        {
            _logger.LogInformation("Mapa {MapId} não encontrado. Criando um novo mapa padrão...", cmd.MapId);
            mapDto = CreateAndSaveDefaultMap(cmd.MapId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico ao carregar o mapa {MapId}", cmd.MapId);
            World.Destroy(cmdEntity);
            return;
        }
        
        if (mapDto != null)
        {
            var mapData = MapData.CreateFromDto(mapDto);
            RegisterMapInWorld(mapData);
        }

        World.Destroy(cmdEntity);
    }
    
    private void RegisterMapInWorld(MapData mapData)
    {
        World.Create(new MapInfo
        {
            MapId = mapData.MapId,
            Name = mapData.Name,
            Width = mapData.Width,
            Height = mapData.Height
        });

        MapIndex.Add(mapData.MapId, mapData);
        _logger.LogInformation("Mapa '{MapName}' ({Width}x{Height}) carregado com sucesso no mundo.", mapData.Name, mapData.Width, mapData.Height);
    }
    
    private MapDto CreateAndSaveDefaultMap(int mapId, bool borderBlocked = true)
    {
        var dto = new MapDto
        {
            MapId = mapId,
            Name = $"Floresta Padrão {mapId}",
            Width = 50,
            Height = 50,
            UsePadded = false
        };

        // ... (lógica de criação do mapa)
        int w = dto.Width;
        int h = dto.Height;
        var tilesRow = new MapData.TileType[w * h];
        var collRow = new byte[w * h];
        Array.Fill(tilesRow, MapData.TileType.Floor);
        if (borderBlocked)
        {
            for (int x = 0; x < w; x++) { collRow[x] = 1; collRow[(h - 1) * w + x] = 1; }
            for (int y = 0; y < h; y++) { collRow[y * w] = 1; collRow[y * w + (w - 1)] = 1; }
        }
        dto.TilesRowMajor = tilesRow;
        dto.CollisionRowMajor = collRow;
        // ...

        var filePath = GetMapFilePath(mapId);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, new JsonSerializerOptions { WriteIndented = true });
        
        Directory.CreateDirectory(_mapsDirectory);
        File.WriteAllBytes(filePath, bytes);
        
        _logger.LogInformation("Mapa padrão salvo em: {FilePath}", filePath);

        return dto;
    }

    // O resto dos métodos (LoadMapDtoFromFile, IsMapLoaded) não precisa de alteração.
    private MapDto LoadMapDtoFromFile(int mapId)
    {
        var filePath = GetMapFilePath(mapId);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Arquivo de mapa não encontrado.", filePath);

        var bytes = File.ReadAllBytes(filePath);
        var dto = JsonSerializer.Deserialize<MapDto>(bytes);
        if (dto == null)
            throw new InvalidDataException($"Falha ao desserializar o mapa: {filePath}");
        
        return dto;
    }
    
    private bool IsMapLoaded(int mapId)
    {
        bool isLoaded = false;
        var query = new QueryDescription().WithAll<MapInfo>();
        World.Query(in query, (ref MapInfo info) =>
        {
            if (info.MapId == mapId)
                isLoaded = true;
        });
        return isLoaded;
    }
}