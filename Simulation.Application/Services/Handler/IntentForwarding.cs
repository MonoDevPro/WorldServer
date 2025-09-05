using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Indexers;
using Simulation.Application.Services.Publisher;
using Simulation.Domain.Templates;

namespace Simulation.Application.Services.Handler;

public sealed class IntentForwarding : ICharIntentHandler, IMapIntentHandler
{
    // agora usa DoubleBufferedCommandBuffer em vez de CommandBuffer
    private readonly DoubleBufferedCommandBuffer _buffer;
    private readonly ICharIndex _charIndex;
    private readonly ICharFactory _charFactory; // mantido caso opte por usar sync factory no main-thread
    private readonly IMapIndex _mapIndex;
    private readonly IMapServiceIndex _mapServiceIndex;
    private readonly IMapFactory _mapFactory;
    private readonly ILogger<IntentForwarding> _logger;

    // precompiled logging delegates (hot-path)
    private static readonly Action<ILogger, int, Exception?> LogMapAlreadyLoaded =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, nameof(LogMapAlreadyLoaded)),
            "Mapa {MapId} já presente, LoadMapIntent ignorado.");

    private static readonly Action<ILogger, int, Exception?> LogCharAlreadyPresent =
        LoggerMessage.Define<int>(LogLevel.Warning, new EventId(2, nameof(LogCharAlreadyPresent)),
            "CharId {CharId} já está no jogo. EnterIntent ignorado.");

    public IntentForwarding(
        DoubleBufferedCommandBuffer buffer,
        ICharIndex charIndex,
        ICharFactory charFactory,
        IMapIndex mapIndex,
        IMapServiceIndex mapServiceIndex,
        IMapFactory mapFactory,
        ILogger<IntentForwarding> logger)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _charIndex = charIndex ?? throw new ArgumentNullException(nameof(charIndex));
        _charFactory = charFactory ?? throw new ArgumentNullException(nameof(charFactory));
        _mapIndex = mapIndex ?? throw new ArgumentNullException(nameof(mapIndex));
        _mapServiceIndex = mapServiceIndex ?? throw new ArgumentNullException(nameof(mapServiceIndex));
        _mapFactory = mapFactory ?? throw new ArgumentNullException(nameof(mapFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // NOTE: cada método apenas enfileira intents para serem processados no tick principal.
    public void HandleIntent(in LoadMapIntent intent, MapTemplate data)
    {
        // Checagem rápida local (melhora UX) — mas não altera o mundo aqui.
        if (_mapServiceIndex.TryGet(intent.MapId, out _))
        {
            LogMapAlreadyLoaded(_logger, intent.MapId, null);
            return;
        }
        if (_mapIndex.TryGet(intent.MapId, out var _))
        {
            LogMapAlreadyLoaded(_logger, intent.MapId, null);
            return;
        }
            
        _mapServiceIndex.Register(intent.MapId, MapService.CreateFromTemplate(data));
        var e = _mapFactory.Create(data); // Cria o mapa no mundo ECS imediatamente (se necessário).
        _buffer.Add(e, intent); // Enfileira o LoadMapIntent para processamento no main-thread.
    }

    public void HandleIntent(in UnloadMapIntent intent)
    {
        if (!_mapServiceIndex.TryGet(intent.MapId, out var _))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Mapa {MapId} não encontrado, UnloadMapIntent ignorado.", intent.MapId);
            return;
        }
        if (_mapIndex.TryGet(intent.MapId, out var mapEntity))
        {
            _mapServiceIndex.Unregister(intent.MapId);
            _buffer.Add(mapEntity, intent); // Enfileira o UnloadMapIntent para processamento no main-thread.
        }
    }

    public void HandleIntent(in EnterIntent intent, CharTemplate template)
    {
        if (_charIndex.TryGet(intent.CharId, out _))
        {
            LogCharAlreadyPresent(_logger, intent.CharId, null);
            return;
        }

        var e = _charFactory.Create(template);
        _buffer.Set(e, intent);
    }

    public void HandleIntent(in ExitIntent intent)
    {
        if (_charIndex.TryGet(intent.CharId, out var entity))
        {
            // Adiciona o intent à entidade para que o PlayerLifecycleSystem o processe no main thread
            _buffer.Add(entity, intent);
        }
    }

    public void HandleIntent(in MoveIntent intent)
    {
        if (_charIndex.TryGet(intent.CharId, out var entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    public void HandleIntent(in AttackIntent intent)
    {
        if (_charIndex.TryGet(intent.CharId, out var entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    public void HandleIntent(in TeleportIntent intent)
    {
        if (_charIndex.TryGet(intent.CharId, out var entity))
        {
            _buffer.Add(entity, intent);
        }
    }

    // Dispose: remover o dispose caso o CommandBuffer seja 'owned' por outro. 
    // DoubleBufferedCommandBuffer é normalmente registrado e gerenciado pelo DI (singleton).
    public void Dispose()
    {
        // Não dispose o buffer se ele for gerenciado pelo DI/owner.
        // Se esta classe for proprietária do buffer, chame: _buffer.Dispose();
    }
}