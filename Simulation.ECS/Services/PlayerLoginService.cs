using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.Application.Ports.Persistence;
using Simulation.Domain.Templates;

namespace Simulation.ECS.Services;

public class PlayerLoginService(IBackgroundTaskQueue taskQueue, IPlayerStagingArea stagingArea)
{
    // Método a ser chamado quando um jogador tenta logar
    public void RequestLogin(int charId)
    {
        Console.WriteLine($"Recebida requisição de login para o CharId: {charId}");

        // Enfileira o trabalho de carregar os dados do banco.
        // Esta chamada é rápida e não bloqueia.
        taskQueue.QueueBackgroundWorkItem(async (serviceProvider, cancellationToken) =>
        {
            // Este código executa no thread do QueuedHostedService.
            var repository = serviceProvider.GetRequiredService<IRepositoryAsync<int, PlayerData>>();
            
            var playerTemplate = await repository.GetAsync(charId, cancellationToken);

            if (playerTemplate != null)
            {
                // Dados carregados! Coloca na staging area para o ECS pegar.
                stagingArea.StageLogin(playerTemplate);
                Console.WriteLine($"Dados para o CharId {charId} carregados e preparados para entrar no mundo.");
            }
            else
            {
                Console.WriteLine($"Falha ao carregar: CharId {charId} não encontrado no banco de dados.");
                // (Aqui você poderia enviar uma mensagem de falha de volta para o jogador)
            }
        });
    }
}