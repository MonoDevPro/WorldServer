namespace Simulation.Network;

public class NetworkOptions
{
    public const string SectionName = "Network";

    public int Port { get; set; } = 27015; // Valor padrão
    public string ConnectionKey { get; set; } = "worldserver-key"; // Valor padrão
    public string ServerAddress { get; set; } = "127.0.0.1"; // Valor padrão para o cliente
}