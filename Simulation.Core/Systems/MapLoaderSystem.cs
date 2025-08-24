using System.Text.Json;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Commons.Components.Map;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

/// <summary>
/// Sistema responsável por carregar mapas na simulação, criando suas entidades
/// e populando os índices espaciais.
/// </summary>
public sealed partial class MapLoaderSystem(World world)
    : BaseSystem<World, float>(world)
{
    private readonly string _mapsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Maps");
    private const string MapPrefix = "map";
    private const string MapExtension = ".mapjson";

    /// <summary>
    /// Retorna o caminho completo para o arquivo de um mapa.
    /// </summary>
    private string GetMapFilePath(int mapId)
        => Path.Combine(_mapsDirectory, $"{MapPrefix}{mapId}{MapExtension}");

    [Query]
    [All<WantsToLoadMap>]
    private void OnLoadMapRequest(in Entity cmdEntity, ref WantsToLoadMap cmd)
    {
        if (IsMapLoaded(cmd.MapId))
        {
            World.Destroy(cmdEntity);
            return;
        }

        // Tenta carregar o DTO do mapa. Se não existir, um DTO padrão será criado e salvo.
        MapDto? mapDto;
        try
        {
            mapDto = LoadMapDtoFromFile(cmd.MapId);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"[MapLoader] Mapa {cmd.MapId} não encontrado. Criando um novo mapa padrão...");
            mapDto = CreateAndSaveDefaultMap(cmd.MapId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MapLoader] Erro crítico ao carregar o mapa {cmd.MapId}: {ex.Message}");
            World.Destroy(cmdEntity);
            return;
        }

        // Com o DTO em mãos, cria o mapa no mundo.
        if (mapDto != null)
        {
            var mapData = MapData.CreateFromDto(mapDto);
            RegisterMapInWorld(mapData);
        }

        World.Destroy(cmdEntity);
    }

    /// <summary>
    /// Carrega o DTO (Data Transfer Object) de um mapa a partir de um arquivo JSON.
    /// </summary>
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

    /// <summary>
    /// Cria um DTO de mapa padrão, salva-o em um arquivo e o retorna.
    /// </summary>
    private MapDto CreateAndSaveDefaultMap(int mapId, bool borderBlocked = true)
    {
        // Define um mapa padrão (ex: 50x50)
        var dto = new MapDto
        {
            MapId = mapId,
            Name = $"Floresta Padrão {mapId}",
            Width = 50,
            Height = 50,
            UsePadded = false // Modo compacto é geralmente melhor para tamanhos não-potência de 2
        };

        int w = dto.Width;
        int h = dto.Height;
        var tilesRow = new MapData.TileType[w * h];
        var collRow = new byte[w * h];

        // Preenche com chão
        Array.Fill(tilesRow, MapData.TileType.Floor);

        // Bloqueia as bordas
        if (borderBlocked)
        {
            for (int x = 0; x < w; x++) { collRow[x] = 1; collRow[(h - 1) * w + x] = 1; } // Linhas de cima e de baixo
            for (int y = 0; y < h; y++) { collRow[y * w] = 1; collRow[y * w + (w - 1)] = 1; } // Colunas da esquerda e direita
        }

        dto.TilesRowMajor = tilesRow;
        dto.CollisionRowMajor = collRow;

        // Salva o novo mapa para uso futuro
        var filePath = GetMapFilePath(mapId);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, new JsonSerializerOptions { WriteIndented = true });
        
        // Garante que o diretório exista
        Directory.CreateDirectory(_mapsDirectory);
        File.WriteAllBytes(filePath, bytes);
        
        Console.WriteLine($"[MapLoader] Mapa padrão salvo em: {filePath}");

        return dto;
    }

    /// <summary>
    /// Cria a entidade do mapa no mundo e registra seus dados nos índices espaciais.
    /// </summary>
    private void RegisterMapInWorld(MapData mapData)
    {
        // Cria a entidade Arch com o componente de informação do mapa.
        World.Create(new MapInfo
        {
            MapId = mapData.MapId,
            Name = mapData.Name,
            Width = mapData.Width,
            Height = mapData.Height
        });

        // Adiciona os dados do mapa (com a ordem Morton) ao índice estático para acesso rápido.
        MapIndex.Add(mapData.MapId, mapData);

        Console.WriteLine($"[MapLoader] Mapa '{mapData.Name}' ({mapData.Width}x{mapData.Height}) carregado com sucesso no mundo.");
    }
    
    /// <summary>
    /// Verifica se um mapa com o ID especificado já existe no mundo.
    /// </summary>
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