using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Enums;
using Simulation.Core.Components;

namespace Simulation.Core.Abstractions.Out;

public readonly record struct CharacterSnapshot(CharId CharId, CharInfo Info, CharState State);
    
    
