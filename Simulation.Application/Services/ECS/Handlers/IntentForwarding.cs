using System.Collections.Concurrent;
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.DTOs.Intents;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.Application.Ports.ECS.Utils.Indexers;
using Simulation.Domain.Templates;

namespace Simulation.Application.Services.ECS.Handlers;

public sealed class IntentForwarding(
    World world,
    IPlayerIndex playerIndex,
    IMapIndex mapIndex,
    ILogger<IntentForwarding> logger)
    : BaseSystem<World, float>(world), IPlayerIntentHandler, IMapIntentHandler
{
    // agora usa DoubleBufferedCommandBuffer em vez de CommandBuffer
    private readonly CommandBuffer _buffer = new CommandBuffer(initialCapacity: 1024);
    
    public override void Update(in float deltaTime)
    {
        // Aplica o buffer na main-thread (antes de outros sistemas que podem depender dos comandos)
        _buffer.Playback(World, dispose: true);
    }
    
    // reservas para evitar duplo-enqueue de EnterIntent com mesmo CharId.
    // valor byte apenas para economia de memória.
    private readonly ConcurrentDictionary<int, byte> _pendingCharReservations = new();

    // precompiled logging delegates (hot-path)
    private static readonly Action<ILogger, int, Exception?> LogMapAlreadyLoaded =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, nameof(LogMapAlreadyLoaded)),
            "Mapa {MapId} já presente, LoadMapIntent ignorado.");
    
    private static readonly Action<ILogger, int, Exception?> LogCharAlreadyReserved =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(2, nameof(LogCharAlreadyReserved)),
            "CharId {CharId} já reservado por outro request; EnterIntent ignorado.");

    private static readonly Action<ILogger, int, Exception?> LogCharAlreadyPresent =
        LoggerMessage.Define<int>(LogLevel.Warning, new EventId(2, nameof(LogCharAlreadyPresent)),
            "CharId {CharId} já está no jogo. EnterIntent ignorado.");

    /// <summary>
    /// Tenta reservar um charId para evitar enqueues concorrentes.
    /// Retorna true se a reserva foi feita (nós somos responsáveis por liberar depois).
    /// </summary>
    public bool TryReserveCharId(int charId) => _pendingCharReservations.TryAdd(charId, 0);

    /// <summary>
    /// Remove a reserva (usado pelo main-thread quando a criação termina ou falha).
    /// Retorna true se a reserva existia e foi removida.
    /// </summary>
    public bool TryRemoveReservation(int charId) => _pendingCharReservations.TryRemove(charId, out _);
    
    // ------------------ Handlers ------------------
    public void HandleIntent(in LoadMapIntent intent, MapTemplate data)
    {
        if (mapIndex.TryGet(intent.MapId, out _))
        {
            LogMapAlreadyLoaded(logger, intent.MapId, null);
            return;
        }

        // Enfileira "LoadMap" command: criamos uma entidade-comando com MapTemplate
        var e = _buffer.Create(new[] { Component<LoadMapIntent>.ComponentType, Component<MapTemplate>.ComponentType });
        _buffer.Set(e, intent);
        _buffer.Set(e, data); // clone se MapTemplate for classe mutável
    }

    public void HandleIntent(in UnloadMapIntent intent)
    {
        if (!mapIndex.TryGet(intent.MapId, out _))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Mapa {MapId} não encontrado, UnloadMapIntent ignorado.", intent.MapId);
            return;
        }

        var e = _buffer.Create(new[] { Component<UnloadMapIntent>.ComponentType });
        _buffer.Set(e, intent);
    }

    public void HandleIntent(in EnterIntent intent)
    {
        // Se já registrado (jogador já no jogo), ignora
        if (playerIndex.TryGet(intent.CharId, out _))
        {
            LogCharAlreadyPresent(logger, intent.CharId, null);
            return;
        }

        // Tenta reservar: se já reservado por outro request, ignora
        if (!TryReserveCharId(intent.CharId))
        {
            LogCharAlreadyReserved(logger, intent.CharId, null);
            return;
        }

        try
        {
            // O servidor vai buscar o template do jogador e construir seu estado inicial
            var e = _buffer.Create(new[] { Component<EnterIntent>.ComponentType });
            _buffer.Set(e, intent);
            // PlayerTemplate será resolvido no PlayerLifecycleSystem a partir do repositório
        }
        catch (Exception ex)
        {
            // Em caso de falha ao enfileirar, limpar a reserva para não bloquear futuro requests
            TryRemoveReservation(intent.CharId);
            logger.LogError(ex, "Falha ao enfileirar EnterIntent para CharId {CharId}", intent.CharId);
            throw;
        }
    }

    public void HandleIntent(in ExitIntent intent)
    {
        if (playerIndex.TryGet(intent.CharId, out var entity) && World.IsAlive(entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    public void HandleIntent(in MoveIntent intent)
    {
        if (playerIndex.TryGet(intent.CharId, out var entity) && World.IsAlive(entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    public void HandleIntent(in AttackIntent intent)
    {
        if (playerIndex.TryGet(intent.CharId, out var entity) && World.IsAlive(entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    public void HandleIntent(in TeleportIntent intent)
    {
        if (playerIndex.TryGet(intent.CharId, out var entity) && World.IsAlive(entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    // Dispose: remover o dispose caso o CommandBuffer seja 'owned' por outro. 
    // DoubleBufferedCommandBuffer é normalmente registrado e gerenciado pelo DI (singleton).
    public override void Dispose()
    {
        _buffer.Dispose();
        _pendingCharReservations.Clear();
        base.Dispose();
    }
}