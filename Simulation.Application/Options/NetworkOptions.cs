namespace Simulation.Application.Options;

public class NetworkOptions
{
    public static string SectionName = "NetworkConfiguration";
    
    public bool AutoReconnect { get; set; } = true;
    public int ConnectDelayMs { get; set; } = 5000;
    public int ReconnectInitialDelayMs { get; set; } = 1000;
    public int ReconnectMaxDelayMs { get; set; } = 15000;
    public int UpdateIntervalMs { get; set; } = 15;
    public int DisconnectTimeoutMs { get; set; } = 5000;
    public bool UseUnsyncedEvents { get; set; } = true;
    
    public string ServerAddress { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 7777;
    public string ConnectionKey { get; set; } = "default_key";
    
    public override string ToString()
    {
        return $"[NetworkOptions: AutoReconnect={AutoReconnect}, ConnectDelayMs={ConnectDelayMs}, " +
               $"ReconnectInitialDelayMs={ReconnectInitialDelayMs}, ReconnectMaxDelayMs={ReconnectMaxDelayMs}, " +
               $"UpdateIntervalMs={UpdateIntervalMs}, DisconnectTimeoutMs={DisconnectTimeoutMs}, " +
               $"UseUnsyncedEvents={UseUnsyncedEvents}, ServerAddress={ServerAddress}, " +
               $"ServerPort={ServerPort}, ConnectionKey={ConnectionKey}]";
    }
}