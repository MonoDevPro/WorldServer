using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Application.Factories;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Domain.Components;

namespace Simulation.Application.Systems;

/// <summary>
/// Sincroniza o CharTemplate no repositório quando os dados de uma entidade no ECS são alterados.
/// </summary>
public sealed partial class TemplateSyncSystem(
    World world,
    ICharTemplateIndex charTemplateIndex,
    ILogger<TemplateSyncSystem> logger)
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<TemplateDirty>] // Query para entidades marcadas
    [All<CharId>]
    private void SyncTemplates(in Entity entity, in CharId charId)
    {
        var charTemplate = charTemplateIndex.TryGet(charId.Value, out var existingTemplate) ? existingTemplate : null;
        if (charTemplate == null)
        {
            // Se o template não existir, nada a fazer
            World.Remove<TemplateDirty>(entity);
            return;
        }
        
        // 1. Atualiza o template com os dados atuais da entidade
        charTemplate = CharFactory.UpdateCharTemplate(World, entity, charTemplate);
        
        logger.LogDebug("TemplateSync: Atualizando template do char {CharId}", charId.Value);

        // 2. Atualiza o template no índice (que serve como nosso "banco de dados" em memória)
        // Nota: A interface IIndex não tem um método 'Update', mas 'Register' funciona como um upsert.

        // 3. Remove o marcador 'dirty' para não reprocessar desnecessariamente
        World.Remove<TemplateDirty>(entity);
    }
}

