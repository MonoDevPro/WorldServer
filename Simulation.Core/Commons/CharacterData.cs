using Simulation.Core.Commons.Enums;

namespace Simulation.Core.Commons;

public class CharacterData
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required Vocation Vocation { get; init; }
    public required Gender Gender { get; init; }
    public required GameVector2 Direction { get; init; }
    public required GameVector2 Position { get; init; }
    public required float Speed { get; init; }
    
    public override string ToString()
    {
        return $"PlayerData(" +
               $"Id: {Id}, " +
               $"Name: {Name}, " +
               $"Vocation: {Vocation}, " +
               $"Gender: {Gender}, " +
               $"Direction: {Direction}, " +
               $"Position: {Position}), " +
               $"Speed: {Speed}, ";
    }
}