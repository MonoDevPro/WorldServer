using Simulation.Application.Ports.Commons.Pools;
using Simulation.Domain.Components;

namespace Simulation.Application.DTOs;

/// <summary>
/// Representa o estado completo de um personagem a ser salvo no banco de dados.
/// Combina dados de base (Template) com o estado atual (Componentes).
/// </summary>
public class CharSaveTemplate
{
    public CharId CharId { get; set; }
    public MapId MapId { get; set; }
    public Position Position { get; set; }
    public Direction Direction { get; set; }
    public MoveStats MoveStats { get; set; }
    public AttackStats AttackStats { get; set; }
    
    public void Populate(CharId charId, MapId mapId, Position position, Direction direction, MoveStats moveStats, AttackStats attackStats)
    {
        this.CharId = charId;
        this.MapId = mapId;
        this.Position = position;
        this.Direction = direction;
        this.MoveStats = moveStats;
        this.AttackStats = attackStats;
    }
}