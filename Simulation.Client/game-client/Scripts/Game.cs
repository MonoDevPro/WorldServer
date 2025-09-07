using Arch.Core;
using Godot;
using Microsoft.Extensions.Logging;
using GameClient.Scripts.Infrastructure;
using GameClient.Scripts.Network;
using GameClient.Scripts.Network.Packets;
using GameClient.Scripts.State;
using GameClient.Scripts.Rendering;
using GameClient.Scripts.Input;
using Simulation.Application.DTOs;

namespace GameClient.Scripts;

public partial class Game : Node
{
    private Node2D _worldRoot = null!;
    private PackedScene _playerScene = GD.Load<PackedScene>("res://Scenes/Player.tscn");
    
    // Core services
    private ServiceContainer _services = null!;
    private World _ecsWorld = null!;
    
    // Game systems
    private SnapshotApplySystem _snapshotSystem = null!;
    private PlayerViewManager _playerViewManager = null!;
    private InputHandler _inputHandler = null!;
    private IntentService _intentService = null!;
    private PacketHandler _packetHandler = null!;
    
    // UI
    private Control _uiRoot = null!;
    private Button _connectButton = null!;
    private Label _statusLabel = null!;
    private Button _testButton = null!;
    
    // State
    private bool _isConnected = false;
    private int _localPlayerId = 1; // Default, will be set by server

    public override void _Ready()
    {
        _worldRoot = GetNode<Node2D>("World");
        
        SetupUI();
        InitializeServices();
        InitializeSystems();
        
        GD.Print("Game initialized successfully");
    }
    
    private void SetupUI()
    {
        // Create UI for connection management
        _uiRoot = new Control();
        AddChild(_uiRoot);
        
        _connectButton = new Button
        {
            Text = "Connect to Server",
            Position = new Vector2(10, 10),
            Size = new Vector2(150, 30)
        };
        _connectButton.Pressed += OnConnectButtonPressed;
        _uiRoot.AddChild(_connectButton);
        
        _statusLabel = new Label
        {
            Text = "Disconnected",
            Position = new Vector2(10, 50),
            Size = new Vector2(200, 30)
        };
        _uiRoot.AddChild(_statusLabel);
        
        // Test button to simulate server responses
        _testButton = new Button
        {
            Text = "Test Player Join",
            Position = new Vector2(10, 90),
            Size = new Vector2(150, 30)
        };
        _testButton.Pressed += OnTestButtonPressed;
        _uiRoot.AddChild(_testButton);
    }
    
    private void InitializeServices()
    {
        _services = new ServiceContainer();
        _ecsWorld = World.Create();
        
        // Register core services
        _services.RegisterSingleton(_ecsWorld);
        _services.RegisterSingleton<ILogger<ClientEventBus>>(GodotLoggerExtensions.CreateGodotLogger<ClientEventBus>());
        
        // Create client event bus
        var eventBus = new ClientEventBus(_services.Get<ILogger<ClientEventBus>>());
        _services.RegisterSingleton<IClientEventBus>(eventBus);
        
        GD.Print("Services initialized");
    }
    
    private void InitializeSystems()
    {
        // Create core systems
        _snapshotSystem = new SnapshotApplySystem(_ecsWorld, _services.Get<IClientEventBus>());
        
        // Create rendering system
        _playerViewManager = new PlayerViewManager();
        _worldRoot.AddChild(_playerViewManager);
        _playerViewManager.Initialize(_snapshotSystem, _ecsWorld);
        
        // Create input handler
        _inputHandler = new InputHandler();
        AddChild(_inputHandler);
        
        GD.Print("Systems initialized");
    }
    
    private async void OnConnectButtonPressed()
    {
        if (_isConnected)
        {
            // Disconnect
            SetConnectionState(false, "Disconnected");
        }
        else
        {
            // Connect
            await AttemptConnection();
        }
    }
    
    private async Task AttemptConnection()
    {
        _statusLabel.Text = "Connecting...";
        _connectButton.Disabled = true;
        
        try
        {
            // Simulate connection delay
            await Task.Delay(1000);
            
            // Simulate successful connection
            SetConnectionState(true, "Connected (Mock)");
            
            // Initialize networking services after connection
            InitializeNetworking();
            
            GD.Print("Connected to server successfully (mock)");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to connect: {ex.Message}");
            SetConnectionState(false, "Connection Failed");
        }
        finally
        {
            _connectButton.Disabled = false;
        }
    }
    
    private void InitializeNetworking()
    {
        // Create networking services with mock implementations
        var mockPacketSender = new MockPacketSender();
        _intentService = new IntentService(mockPacketSender);
        _intentService.SetLocalPlayerId(_localPlayerId);
        
        // Create packet handler
        _packetHandler = new PacketHandler(_services.Get<IClientEventBus>());
        _packetHandler.Initialize();
        
        // Initialize input handler with intent service
        _inputHandler.Initialize(_intentService);
        _inputHandler.SetConnected(true);
        
        // Set local player ID in view manager
        _playerViewManager.SetLocalPlayerId(_localPlayerId);
        
        // Send initial enter intent
        _intentService.SendEnterIntent();
    }
    
    private void SetConnectionState(bool connected, string status)
    {
        _isConnected = connected;
        _statusLabel.Text = status;
        _connectButton.Text = connected ? "Disconnect" : "Connect to Server";
        
        if (_inputHandler != null)
        {
            _inputHandler.SetConnected(connected);
        }
    }
    
    // Test method to simulate server responses
    private void OnTestButtonPressed()
    {
        if (!_isConnected || _packetHandler == null)
            return;
            
        GD.Print("Simulating server response...");
        
        // Simulate a JoinAckSnapshot
        var joinAckSnapshot = new ClientJoinAckSnapshot
        {
            YourCharId = _localPlayerId,
            MapId = 1,
            Others = new List<PlayerState>
            {
                new PlayerState
                {
                    CharId = 2,
                    MapId = 1,
                    Position = new Simulation.Domain.Components.Position { X = 5, Y = 5 },
                    Direction = new Simulation.Domain.Components.Direction { X = 0, Y = 1 },
                    MoveSpeed = 1.0f,
                    AttackCastTime = 0.5f,
                    AttackCooldown = 1.0f
                }
            }
        };
        
        _packetHandler.HandleJoinAckSnapshot(joinAckSnapshot);
        
        // After a delay, simulate a move
        GetTree().CreateTimer(2.0).Timeout += () =>
        {
            var moveSnapshot = new ClientMoveSnapshot
            {
                CharId = 2,
                Old = new Simulation.Domain.Components.Position { X = 5, Y = 5 },
                New = new Simulation.Domain.Components.Position { X = 6, Y = 5 }
            };
            _packetHandler.HandleMoveSnapshot(moveSnapshot);
        };
    }

    public override void _Process(double delta)
    {
        // Update systems here if needed
    }
    
    public override void _ExitTree()
    {
        _snapshotSystem?.Dispose();
        _ecsWorld?.Dispose();
    }
}
