using Simulation.Domain.Components;

namespace Simulation.Domain.Templates;

public enum Gender : int { None, Male, Female }
public enum Vocation : int { None, Mage, Archer }

public class PlayerTemplate
{
    public string Name = string.Empty;
    public Gender Gender;
    public Vocation Vocation;
    
    public int CharId;
    public int MapId;
    public Position Position;
    public Direction Direction;
    public float MoveSpeed;
    public float AttackCastTime;
    public float AttackCooldown;
}