using Microsoft.Extensions.Logging;
using Simulation.Client.Core;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Client.Core;

/// <summary>
/// Gerenciador de entrada do usuário que converte comandos de console em intents.
/// Coordena a interação do jogador com o jogo.
/// </summary>
public class InputManager : IDisposable
{
    private readonly IIntentSender _intentSender;
    private readonly ILogger<InputManager> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private int _currentCharId = -1;
    private bool _disposed;

    public InputManager(IIntentSender intentSender, ILogger<InputManager> logger)
    {
        _intentSender = intentSender;
        _logger = logger;
    }

    public int CurrentCharId 
    { 
        get => _currentCharId; 
        set => _currentCharId = value; 
    }

    /// <summary>
    /// Inicia o loop de entrada do usuário
    /// </summary>
    public async Task StartInputLoopAsync(CancellationToken cancellationToken = default)
    {
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _cancellationTokenSource.Token);

        _logger.LogInformation("Iniciando loop de entrada do usuário...");
        _logger.LogInformation("Comandos disponíveis:");
        _logger.LogInformation("  enter <charId> - Entrar no jogo com o personagem");
        _logger.LogInformation("  exit - Sair do jogo");
        _logger.LogInformation("  move <x> <y> - Mover para direção (ex: move 1 0 para direita)");
        _logger.LogInformation("  attack - Atacar");
        _logger.LogInformation("  teleport <mapId> <x> <y> - Teleportar para posição");
        _logger.LogInformation("  quit - Sair do cliente");
        _logger.LogInformation("");

        try
        {
            while (!combinedCts.Token.IsCancellationRequested)
            {
                Console.Write("> ");
                var input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                ProcessCommand(input.Trim());
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Loop de entrada cancelado");
        }
    }

    private void ProcessCommand(string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var cmd = parts[0].ToLowerInvariant();

        try
        {
            switch (cmd)
            {
                case "enter":
                    if (parts.Length < 2 || !int.TryParse(parts[1], out var charId))
                    {
                        Console.WriteLine("Uso: enter <charId>");
                        return;
                    }
                    HandleEnterCommand(charId);
                    break;

                case "exit":
                    HandleExitCommand();
                    break;

                case "move":
                    if (parts.Length < 3 || !int.TryParse(parts[1], out var x) || !int.TryParse(parts[2], out var y))
                    {
                        Console.WriteLine("Uso: move <x> <y> (ex: move 1 0)");
                        return;
                    }
                    HandleMoveCommand(x, y);
                    break;

                case "attack":
                    HandleAttackCommand();
                    break;

                case "teleport":
                    if (parts.Length < 4 || !int.TryParse(parts[1], out var mapId) || 
                        !int.TryParse(parts[2], out var posX) || !int.TryParse(parts[3], out var posY))
                    {
                        Console.WriteLine("Uso: teleport <mapId> <x> <y>");
                        return;
                    }
                    HandleTeleportCommand(mapId, posX, posY);
                    break;

                case "quit":
                    _cancellationTokenSource.Cancel();
                    break;

                default:
                    Console.WriteLine($"Comando desconhecido: {cmd}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar comando: {Command}", command);
            Console.WriteLine($"Erro ao processar comando: {ex.Message}");
        }
    }

    private void HandleEnterCommand(int charId)
    {
        _currentCharId = charId;
        var intent = new EnterIntent(charId);
        _intentSender.SendIntent(intent);
        _logger.LogInformation("Enviado EnterIntent para CharId {CharId}", charId);
        Console.WriteLine($"Tentando entrar com personagem {charId}...");
    }

    private void HandleExitCommand()
    {
        if (_currentCharId == -1)
        {
            Console.WriteLine("Nenhum personagem ativo");
            return;
        }

        var intent = new ExitIntent(_currentCharId);
        _intentSender.SendIntent(intent);
        _logger.LogInformation("Enviado ExitIntent para CharId {CharId}", _currentCharId);
        Console.WriteLine($"Saindo com personagem {_currentCharId}...");
        _currentCharId = -1;
    }

    private void HandleMoveCommand(int x, int y)
    {
        if (_currentCharId == -1)
        {
            Console.WriteLine("Entre no jogo primeiro (use: enter <charId>)");
            return;
        }

        var input = new Input { X = x, Y = y };
        var intent = new MoveIntent(_currentCharId, input);
        _intentSender.SendIntent(intent);
        _logger.LogTrace("Enviado MoveIntent para CharId {CharId}: ({X}, {Y})", _currentCharId, x, y);
        Console.WriteLine($"Movendo personagem {_currentCharId} para direção ({x}, {y})");
    }

    private void HandleAttackCommand()
    {
        if (_currentCharId == -1)
        {
            Console.WriteLine("Entre no jogo primeiro (use: enter <charId>)");
            return;
        }

        var intent = new AttackIntent(_currentCharId);
        _intentSender.SendIntent(intent);
        _logger.LogInformation("Enviado AttackIntent para CharId {CharId}", _currentCharId);
        Console.WriteLine($"Personagem {_currentCharId} está atacando!");
    }

    private void HandleTeleportCommand(int mapId, int x, int y)
    {
        if (_currentCharId == -1)
        {
            Console.WriteLine("Entre no jogo primeiro (use: enter <charId>)");
            return;
        }

        var position = new Position { X = x, Y = y };
        var intent = new TeleportIntent(_currentCharId, mapId, position);
        _intentSender.SendIntent(intent);
        _logger.LogInformation("Enviado TeleportIntent para CharId {CharId} para MapId {MapId} ({X}, {Y})", 
            _currentCharId, mapId, x, y);
        Console.WriteLine($"Teleportando personagem {_currentCharId} para mapa {mapId} em ({x}, {y})");
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _disposed = true;
    }
}