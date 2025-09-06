using Godot;

namespace GameClient.Scripts.Config;

public class ClientConfig
{
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 27015;
    public string Key { get; init; } = "worldserver-key-from-json";

    public static ClientConfig Load()
    {
        // Placeholder: in future read from file (user://client.json)
        // If file exists parse JSON, else return defaults.
        return new ClientConfig();
    }
}