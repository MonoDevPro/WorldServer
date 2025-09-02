using System.Collections.Concurrent;
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Factories;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.Systems.In;

/// <summary>
/// Este sistema atua como a ponte entre a entrada externa (rede) e o mundo ECS.
/// Ele implementa IIntentHandler para receber intents de forma thread-safe e,
/// como um BaseSystem, processa esses intents de forma sincronizada com o pipeline do ECS.
/// </summary>
public class CharIntentsHandlerSystem : BaseSystem<World, float>, ICharIntentHandler
{
    private readonly ILogger<CharIntentsHandlerSystem> _logger;
    private readonly ICharIndex _charIndex;
    private readonly ICharTemplateIndex _charTemplateIndex;
    private readonly ICharTemplateRepository _charTemplateRepository;
    private readonly CommandBuffer _cmd = new(256);

    // Usamos ConcurrentQueue para garantir que a rede possa enfileirar intents
    // de forma segura a partir de qualquer thread.
    private readonly ConcurrentQueue<EnterIntent> _enterQueue = new();
    private readonly ConcurrentQueue<ExitIntent> _exitQueue = new();
    private readonly ConcurrentQueue<MoveIntent> _moveQueue = new();
    private readonly ConcurrentQueue<AttackIntent> _attackQueue = new();
    private readonly ConcurrentQueue<TeleportIntent> _teleportQueue = new();

    public CharIntentsHandlerSystem(World world, ILogger<CharIntentsHandlerSystem> logger, 
        ICharIndex charIndex, 
        ICharTemplateIndex charTemplateIndex,
        ICharTemplateRepository charTemplateRepository)
        : base(world)
    {
        _logger = logger;
        _charIndex = charIndex;
        _charTemplateIndex = charTemplateIndex;
        _charTemplateRepository = charTemplateRepository;
    }

    // --- Implementação da Interface IIntentHandler (Porta de Entrada) ---
    // Estes métodos são chamados pela camada de rede. Eles APENAS enfileiram.
    public void HandleIntent(in EnterIntent intent) => _enterQueue.Enqueue(intent);
    public void HandleIntent(in ExitIntent intent) => _exitQueue.Enqueue(intent);
    public void HandleIntent(in MoveIntent intent) => _moveQueue.Enqueue(intent);
    public void HandleIntent(in AttackIntent intent) => _attackQueue.Enqueue(intent);
    public void HandleIntent(in TeleportIntent intent) => _teleportQueue.Enqueue(intent);

    // --- Lógica do Sistema ECS ---
    // Este método é chamado a cada tick pelo SimulationRunner.
    public override void Update(in float delta)
    {
        // Processa todas as filas e agenda comandos no CommandBuffer
        ConsumeEnterIntents();
        ConsumeExitIntents();
        ConsumeMoveIntents();
        ConsumeAttackIntents();
        ConsumeTeleportIntents();

        // Aplica todas as mudanças agendadas ao mundo de uma só vez.
        _cmd.Playback(World, dispose: true);
    }

    private void ConsumeEnterIntents()
    {
        while (_enterQueue.TryDequeue(out var intent))
        {
            // Evita que um jogador entre duas vezes
            if (_charIndex.TryGet(intent.CharId, out _))
            {
                _logger.LogWarning("CharId {id} já está no jogo. EnterIntent ignorado.", intent.CharId);
                continue;
            }
            
            if (!_charTemplateRepository.TryGet(intent.CharId, out var existing) || existing == null)
            {
                // Cria um template padrão se não existir
                var ch = new CharTemplate
                {
                    CharId = intent.CharId,
                    MapId = 1,
                    Name = $"Player{intent.CharId}",
                    Position = new Position { X = 5, Y = 5 },
                    Direction = new Direction { X = 0, Y = 1 },
                    Gender = Gender.Male,
                    Vocation = Vocation.Mage,
                    AttackCastTime = 1f,
                    AttackCooldown = 2f,
                    MoveSpeed = 1f,
                };
                _charTemplateRepository.Add(ch.CharId, ch);
                _charTemplateIndex.Register(ch.CharId, ch);
                _logger.LogInformation("SeedChars: Criado novo CharTemplate para CharId={CharId}, Name={Name}", ch.CharId, ch.Name);
            }
            else
            {
                _logger.LogDebug("SeedChars: ICharTemplateIndex already had CharId={CharId}; skipping register.", intent.CharId);
                _charTemplateIndex.Register(existing.CharId, existing); // Ensure it's registered
            }
            // Load CharTemplate from repository
            if (!_charTemplateIndex.TryGet(intent.CharId, out var template) || template is null)
            {
                _logger.LogError("CharTemplate para CharId {id} não encontrado no repositório. EnterIntent ignorado.", intent.CharId);
                continue;
            }

            // 2. Agenda a criação da entidade no ECS
            var entity = CharFactory.CreateEntity(_cmd, template);

            // 3. Adiciona o intent como um componente para ser processado pelo PlayerLifecycleSystem
            _cmd.Add(entity, intent);
        }
    }

    private void ConsumeExitIntents()
    {
        while (_exitQueue.TryDequeue(out var intent))
        {
            if (_charIndex.TryGet(intent.CharId, out var entity))
            {
                // Adiciona o intent à entidade para que o PlayerLifecycleSystem o processe
                _cmd.Add(entity, intent);
            }
        }
    }
    
    private void ConsumeMoveIntents()
    {
        while (_moveQueue.TryDequeue(out var intent))
        {
            if (_charIndex.TryGet(intent.CharId, out var entity))
            {
                // Adiciona o componente de ação de movimento
                _cmd.Add(entity, intent);
            }
        }
    }
    
    private void ConsumeAttackIntents()
    {
        while (_attackQueue.TryDequeue(out var intent))
        {
            if (_charIndex.TryGet(intent.CharId, out var entity))
            {
                // Adiciona o componente de ação de ataque
                _cmd.Add(entity, intent);
            }
        }
    }
    
    private void ConsumeTeleportIntents()
    {
        while (_teleportQueue.TryDequeue(out var intent))
        {
            if (_charIndex.TryGet(intent.CharId, out var entity))
            {
                // Adiciona o componente de ação de teleporte
                _cmd.Add(entity, intent);
            }
        }
    }
    
}