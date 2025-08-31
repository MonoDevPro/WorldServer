# WorldServer Client Implementation

## Overview

This project demonstrates a complete client-server architecture built with **ArchECS** and following **"Ports & Adapters"** design patterns. The client has been successfully implemented with all required features.

## Architecture Highlights

### ✅ Excellent "Ports & Adapters" Design
- **Clean separation** between interfaces (Ports) and implementations (Adapters)
- **IIntentSender** / **LiteNetClient**: Network communication abstraction
- **ISnapshotHandler** / **SnapshotHandlerSystem**: Server state synchronization
- **Flexible and testable** architecture that's easy to maintain

### ✅ Client-Side ECS with ArchECS
- **SnapshotHandlerSystem**: Processes server snapshots and updates client ECS world
- **CharFactory integration**: Uses same entity creation patterns as server
- **60fps tick rate**: Consistent with server simulation loop
- **CommandBuffer pattern**: Batched ECS updates for performance

### ✅ Robust Networking
- **LiteNetLib integration**: Reliable UDP communication
- **Connection retry logic**: Handles temporary network issues
- **Thread-safe operations**: Network and ECS systems properly isolated
- **Graceful disconnect handling**: Clean connection lifecycle management

### ✅ User Interface
- **Console-based commands**: Simple and intuitive interaction
- **Available commands**:
  - `enter <charId>` - Enter the game world
  - `move <x> <y>` - Move character (e.g., `move 1 0` for right)
  - `attack` - Perform attack action
  - `teleport <mapId> <x> <y>` - Teleport to location
  - `exit` - Leave the game
  - `quit` - Close client

## Demonstration Results

### ✅ Successful Client-Server Communication
```
=== Cliente WorldServer ===
info: Cliente conectado ao servidor com sucesso
Comandos disponíveis:
> enter 1
Tentando entrar com personagem 1...
> move 1 0  
Movendo personagem 1 para direção (1, 0)
> attack
Personagem 1 está atacando!
> quit
```

### ✅ Server Processing
- Client connection detected and established
- All intents received and processed by server
- Proper intent validation and ECS integration
- Clean disconnection handling

## Technical Implementation

### Core Components

1. **ClientLoop** - Main client execution loop
   - Network polling and connection management
   - ECS world updates at 60fps
   - User input coordination

2. **LiteNetClient** - Network communication adapter
   - Implements `IIntentSender` for sending commands
   - Handles `ISnapshotHandler` callbacks for server updates
   - Connection lifecycle management

3. **SnapshotHandlerSystem** - ECS system for server state sync
   - Processes various snapshot types (Enter, Move, Attack, etc.)
   - Maintains character entity mapping
   - Updates client world state

4. **InputManager** - Console command processor
   - Parses user commands and converts to intents
   - Provides help and error handling
   - Coordinates with active character state

### Dependency Injection Setup
```csharp
services.AddSimulationClient(configuration);
// Registers: World, SnapshotHandlerSystem, LiteNetClient, 
//           ISnapshotHandler, IIntentSender, InputManager, ClientLoop
```

## Benefits of This Architecture

1. **Maintainability**: Clear separation of concerns with well-defined interfaces
2. **Testability**: Each component can be tested in isolation
3. **Extensibility**: New features can be added without breaking existing code
4. **Performance**: ECS pattern provides efficient component management
5. **Consistency**: Client follows exact same patterns as server implementation

## Running the Demo

### Start Server
```bash
cd WorldServer
dotnet run --project Simulation.Console
```

### Start Client
```bash
cd WorldServer  
dotnet run --project Simulation.Client
```

### Example Session
```
> enter 1        # Enter with character ID 1
> move 1 0       # Move right
> move 0 1       # Move up
> attack         # Perform attack
> teleport 1 5 5 # Teleport to map 1, position (5,5)
> exit           # Leave game
> quit           # Close client
```

## Conclusion

The client implementation successfully demonstrates the power of the **"Ports & Adapters"** architecture combined with **ArchECS**. The result is a clean, maintainable, and extensible client that mirrors the excellent patterns established in the server, providing a solid foundation for game development with real-time networking capabilities.