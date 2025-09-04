using Simulation.Application.Ports.Commons.Pools;
using Simulation.Domain.Components;

namespace Simulation.Application.DTOs;

/// <summary>
/// Representa o estado completo de um personagem a ser salvo no banco de dados.
/// Combina dados de base (Template) com o estado atual (Componentes).
/// </summary>
public class CharSaveTemplate : IResetable
{
    public CharId CharId { get; set; }
    public MapId MapId { get; set; }
    public Position Position { get; set; }
    public Direction Direction { get; set; }
    public MoveStats MoveStats { get; set; }
    public AttackStats AttackStats { get; set; }
    
    public CharSaveTemplate Populate(CharId charId, MapId mapId, Position position, Direction direction, MoveStats moveStats, AttackStats attackStats)
    {
        return new CharSaveTemplate
        {
            CharId = charId,
            MapId = mapId,
            Position = position,
            Direction = direction,
            MoveStats = moveStats,
            AttackStats = attackStats
        };
    }
    
    public void Reset()
    {
        CharId = default;
        MapId = default;
        Position = default;
        Direction = default;
        MoveStats = default;
        AttackStats = default;
    }
}