namespace Simulation.Application.Ports.Commons.Pools;

/// <summary>
/// Define o contrato para um pool de objetos genérico.
/// A lógica do jogo dependerá desta interface, não de uma implementação concreta.
/// </summary>
/// <typeparam name="T">O tipo do objeto a ser poolado.</typeparam>
public interface IObjectPool<T> where T : class
{
    T Get();
    void Return(T obj);
}