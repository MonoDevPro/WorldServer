using Simulation.Core.Abstractions.Commons.Enums;

namespace Simulation.Core.Abstractions.Commons.Components.Char;

public struct CharInfo
{
    public IntPtr NamePtr; // pointer para memória não-gerenciada
    public int NameLength; // comprimento em bytes (UTF-8 por exemplo)
    public Gender Gender;
    public Vocation Vocation;
}