using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Char.Factories;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Factories;
using Simulation.Application.Ports.Map.Indexers;
using Simulation.Domain.Templates;

namespace Simulation.Application.Services.Handlers;

/// <summary>
/// Este sistema atua como a ponte entre a entrada externa (rede) e o mundo ECS.
/// Ele implementa IIntentHandler para receber intents de forma thread-safe e,
/// como um BaseSystem, processa esses intents de forma sincronizada com o pipeline do ECS.
/// </summary>
public class IntentForwarding(
    CommandBuffer buffer,
    ICharIndex charIndex,
    ICharFactory charFactory,
    IMapIndex mapIndex,
    IMapFactory mapFactory,
    ILogger<IntentForwarding> logger) 
    : ICharIntentHandler, IMapIntentHandler
{
    public void HandleIntent(in LoadMapIntent intent, MapTemplate data)
    {
        // Evita carregar um mapa que já está no jogo
        if (mapIndex.TryGet(intent.MapId, out _))
        {
            logger.LogDebug("Mapa {MapId} já presente, LoadMapIntent ignorado.", intent.MapId);
            return;
        }
        mapIndex.Register(intent.MapId, MapService.CreateFromTemplate(data));
        
        // Usa a fábrica injetada para agendar a criação da entidade
        var mapEntity = mapFactory.Create(data);
        // Adiciona o intent como um componente para ser processado por um sistema de ciclo de vida
        buffer.Add(mapEntity, intent);
    }
    public void HandleIntent(in UnloadMapIntent intent)
    {
        // Verifica se o mapa existe antes de tentar descarregá-lo
        if (!mapIndex.TryGet(intent.MapId, out var mapService))
        {
            logger.LogDebug("Mapa {MapId} não encontrado, UnloadMapIntent ignorado.", intent.MapId);
            return;
        }
        mapIndex.Unregister(intent.MapId);
        
        // Adiciona o intent como um componente para ser processado por um sistema de ciclo de vida
        var e = buffer.Create([Component<UnloadMapIntent>.ComponentType]);
        buffer.Set(e, intent);
    }
    public void HandleIntent(in EnterIntent intent, CharTemplate template)
    {
        // Evita que um jogador entre duas vezes
        if (charIndex.TryGet(intent.CharId, out _))
        {
            logger.LogWarning("CharId {id} já está no jogo. EnterIntent ignorado.", intent.CharId);
            return;
        }
        
        var entity = charFactory.Create(template);
        buffer.Add(entity, intent);
    }
    public void HandleIntent(in ExitIntent intent)
    {
        if (charIndex.TryGet(intent.CharId, out var entity))
        {
            // Adiciona o intent à entidade para que o PlayerLifecycleSystem o processe
            buffer.Add(entity, intent);
        }
    }
    public void HandleIntent(in MoveIntent intent)
    {
        if (charIndex.TryGet(intent.CharId, out var entity))
        {
            // Adiciona o componente de ação de movimento
            buffer.Add(entity, intent);
        }
    }
    public void HandleIntent(in AttackIntent intent)
    {
        if (charIndex.TryGet(intent.CharId, out var entity))
        {
            // Adiciona o componente de ação de ataque
            buffer.Add(entity, intent);
        }
    }
    public void HandleIntent(in TeleportIntent intent)
    {
        if (charIndex.TryGet(intent.CharId, out var entity))
        {
            // Adiciona o componente de ação de teleporte
            buffer.Add(entity, intent);
        }
    }
    
    public void Dispose()
    {
        buffer.Dispose();
    }
}