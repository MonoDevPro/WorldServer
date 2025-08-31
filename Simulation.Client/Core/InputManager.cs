using Microsoft.Extensions.Logging;
using Simulation.Client.Core;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Client.Core;

/// <summary>
/// Gerenciador de entrada do usuário que converte comandos de console em intents.
/// Adaptado para um loop de jogo não bloqueante.
/// </summary>
public class InputManager
{
    private readonly IIntentSender _intentSender;
    private readonly ILogger<InputManager> _logger;
    private int _currentCharId = -1;

    public InputManager(IIntentSender intentSender, ILogger<InputManager> logger)
    {
        _intentSender = intentSender;
        _logger = logger;
        
        // Imprime as instruções uma vez no início
        PrintInstructions();
    }
    
    /// <summary>
    /// Verifica e processa a entrada do usuário. Deve ser chamado a cada frame do loop principal.
    /// </summary>
    public void ProcessInput(CancellationTokenSource source)
    {
        // Usa Console.KeyAvailable para não bloquear o loop do jogo
        if (!Console.KeyAvailable)
            return;

        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.Write("> ");
            return;
        }

        ProcessCommand(input.Trim(), source);
        Console.Write("> ");
    }
    
    private void ProcessCommand(string command, CancellationTokenSource source)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var cmd = parts[0].ToLowerInvariant();
        
        switch (cmd)
        {
            case "enter":
                if (parts.Length < 2 || !int.TryParse(parts[1], out var charId))
                {
                    Console.WriteLine("Uso: enter <charId>");
                    return;
                }
                _currentCharId = charId;
                _intentSender.SendIntent(new EnterIntent(charId));
                Console.WriteLine($"A entrar no jogo com CharId {charId}...");
                break;
            case "exit":
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
                break;

            case "move":
                if (parts.Length < 3 || !int.TryParse(parts[1], out var x) || !int.TryParse(parts[2], out var y))
                {
                    Console.WriteLine("Uso: move <x> <y> (ex: move 1 0)");
                    return;
                }
                if (_currentCharId == -1)
                {
                    Console.WriteLine("Entre no jogo primeiro (use: enter <charId>)");
                    return;
                }

                var input = new Input { X = x, Y = y };
                var intent1 = new MoveIntent(_currentCharId, input);
                _intentSender.SendIntent(intent1);
                _logger.LogTrace("Enviado MoveIntent para CharId {CharId}: ({X}, {Y})", _currentCharId, x, y);
                Console.WriteLine($"Movendo personagem {_currentCharId} para direção ({x}, {y})");
                break;

            case "attack":
                if (_currentCharId == -1)
                {
                    Console.WriteLine("Entre no jogo primeiro (use: enter <charId>)");
                    return;
                }

                var intent2 = new AttackIntent(_currentCharId);
                _intentSender.SendIntent(intent2);
                _logger.LogInformation("Enviado AttackIntent para CharId {CharId}", _currentCharId);
                Console.WriteLine($"Personagem {_currentCharId} está atacando!");
                break;

            case "teleport":
                if (parts.Length < 4 || !int.TryParse(parts[1], out var mapId) || 
                    !int.TryParse(parts[2], out var posX) || !int.TryParse(parts[3], out var posY))
                {
                    Console.WriteLine("Uso: teleport <mapId> <x> <y>");
                    return;
                }
                if (_currentCharId == -1)
                {
                    Console.WriteLine("Entre no jogo primeiro (use: enter <charId>)");
                    return;
                }

                var position = new Position { X = posX, Y = posY };
                var intent3 = new TeleportIntent(_currentCharId, mapId, position);
                _intentSender.SendIntent(intent3);
                _logger.LogInformation("Enviado TeleportIntent para CharId {CharId} para MapId {MapId} ({X}, {Y})", 
                    _currentCharId, mapId, posX, posY);
                Console.WriteLine($"Teleportando personagem {_currentCharId} para mapa {mapId} em ({posX}, {posY})");
                break;

            case "quit":
                source.Cancel();
                break;
            default:
                Console.WriteLine($"Comando desconhecido: {cmd}");
                break;
        }
    }

    private void PrintInstructions()
    {
        _logger.LogInformation("Comandos disponíveis:");
        _logger.LogInformation("  enter <charId> - Entrar no jogo com o personagem");
        _logger.LogInformation("  move <x> <y> - Mover (ex: move 1 0)");
        _logger.LogInformation("  attack - Atacar");
        _logger.LogInformation("  exit - Sair do jogo");
        _logger.LogInformation("  quit - Sair da aplicação");
        Console.Write("> ");
    }
}