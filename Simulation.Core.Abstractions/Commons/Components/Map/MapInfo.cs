namespace Simulation.Core.Abstractions.Commons.Components.Map;

/// <summary>
/// Armazena dados de identificação estáticos de uma entidade de mapa.
/// </summary>
public struct MapInfo
{
    /// <summary>
    /// O ID numérico único do mapa (ex: 1 para Floresta, 2 para Cidade).
    /// </summary>
    public int MapId;

    /// <summary>
    /// O nome de exibição do mapa.
    /// </summary>
    public string Name;

    public int Width;
    public int Height;
    public bool UsePadded;
}