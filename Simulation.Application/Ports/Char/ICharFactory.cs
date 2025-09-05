using Arch.Core;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons.Factories;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Char;

public interface ICharFactory : 
    IFactory<Entity, CharTemplate>, 
    IArchetypeProvider<CharTemplate>, 
    IQueryProvider<CharTemplate>,
    IUpdateFromRuntime<CharTemplate, Entity>,
    IUpdateFromRuntime<CharSaveTemplate, Entity>
{
    
}