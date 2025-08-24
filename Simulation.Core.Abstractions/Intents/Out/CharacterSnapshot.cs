using Simulation.Core.Abstractions.Commons.Components.Char;

namespace Simulation.Core.Abstractions.Intents.Out;

public readonly record struct CharacterSnapshot(CharId CharId, CharInfo Info, CharState State);
    
    
