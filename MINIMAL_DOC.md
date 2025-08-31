### Documentação do Projeto: WorldServer

Bem-vindo ao **WorldServer**! Este documento fornece uma visão geral da arquitetura, das tecnologias e dos principais conceitos do projeto para o ajudar a começar.

#### 1. Visão Geral

O WorldServer é um backend autoritativo para um jogo multiplayer em tempo real, construído com uma arquitetura moderna e de alta performance em .NET. Ele utiliza um padrão **Entity Component System (ECS)** para gerir o estado da simulação e um design de **Ports & Adapters** para manter a lógica do jogo desacoplada dos serviços externos, como a rede e o acesso a dados.

**O que o projeto oferece:**
* Uma base sólida para um jogo multiplayer 2D baseado em grid (tile-based).
* Um pipeline de simulação de *tick* fixo, garantindo um comportamento determinista e consistente.
* Gerenciamento do ciclo de vida do jogador (entrada e saída).
* Sistemas de jogo implementados, incluindo movimento baseado em grid, ataques e teleporte.
* Buscas espaciais eficientes utilizando uma QuadTree para encontrar entidades próximas.
* Uma camada de rede desacoplada usando **LiteNetLib** para comunicação UDP.

#### 2. Arquitetura Principal

O projeto é construído sobre dois pilares arquitetónicos:

* **Ports & Adapters (Arquitetura Hexagonal):** A lógica central da simulação (`Simulation.Core`) não sabe nada sobre o mundo exterior. Ela define "Portas" (`Ports`), que são interfaces C# (ex: `IIntentHandler`, `ISnapshotPublisher`). As camadas externas, como a rede, fornecem implementações concretas ("Adaptadores") para essas portas. Isso torna a lógica do jogo independente da tecnologia de rede, do banco de dados, etc.
* **Entity Component System (ECS):** Utiliza o framework **ArchECS**. Em vez de objetos com dados e métodos, o estado do jogo é representado por:
    * **Entidades**: Simples identificadores (IDs).
    * **Componentes**: `Structs` de dados puros que representam um aspeto de uma entidade (ex: `Position`, `Health`).
    * **Sistemas**: A lógica do jogo que itera sobre conjuntos de componentes para atualizar o estado do mundo.

#### 3. Estrutura do Projeto

A solução está dividida em vários projetos, cada um com uma responsabilidade clara:

* **`Simulation.Core.Abstractions`**: Projeto central e partilhado. Contém todas as definições de dados (componentes, intents, snapshots) e as interfaces ("Ports"). Não tem lógica, apenas contratos.
* **`Simulation.Core`**: O coração da simulação. Contém todos os sistemas ECS (movimento, ataque, etc.) e as implementações dos "Adaptadores" internos, como os índices.
* **`Simulation.Network`**: A camada de rede. Contém os "Adaptadores" que implementam as portas de rede usando LiteNetLib, como o `LiteNetPublisher`.
* **`Simulation.Console`**: O ponto de entrada executável do servidor. É responsável pela configuração, injeção de dependência e por iniciar o loop principal da simulação.
* **`Simulation.Client`**: Um cliente de consola básico para testes.
* **`Simulation.Core.Tests`**: Projeto para testes unitários e de integração do `Simulation.Core`.

#### 4. O Fluxo de Dados

O servidor opera num ciclo contínuo (tick), seguindo um fluxo de dados claro:

1.  **Entrada (Input):** O `LiteNetServer` recebe pacotes UDP. Ele desserializa-os para `Intents` (ex: `MoveIntent`) e passa-os para o `IntentsHandlerSystem`.
2.  **Simulação (ECS Pipeline):** O `SimulationPipeline` executa uma lista ordenada de sistemas a cada *tick*.
    * Sistemas de lógica (`GridMovementSystem`, `AttackSystem`) processam os intents e modificam os componentes das entidades.
    * Sistemas de sincronização (`SpatialIndexSyncSystem`) atualizam estruturas de dados auxiliares, como a QuadTree.
3.  **Saída (Output):** Quando um sistema de lógica executa uma ação importante, ele dispara um evento (ex: `MoveSnapshot`) no `EventBus` do Arch. O `SnapshotPublisherSystem` captura esses eventos e os encaminha para o `LiteNetPublisher`, que os serializa e envia pela rede para os clientes.

#### 5. Como Começar

1.  **Executar o Servidor:** O projeto inicializável é o `Simulation.Console`. Basta executá-lo. Ele irá ler o `appsettings.json` para as configurações de rede e do mundo.
2.  **Entender o Fluxo:** O melhor ponto de partida para entender o código é o ficheiro `Simulation.Core/SimulationPipeline.cs`. Ele mostra a ordem exata em que toda a lógica do jogo é executada.
3.  **Adicionar uma Nova Funcionalidade:** Para adicionar uma nova mecânica de jogo (ex: um sistema de magia):
    * Defina os novos **componentes** em `Simulation.Core.Abstractions/Commons/Components.cs`.
    * Defina os **intents** e **snapshots** de rede em `Simulation.Core.Abstractions/Adapters/`.
    * Crie um novo **sistema** em `Simulation.Core/Systems/` que contenha a lógica.
    * Registe o novo sistema em `Simulation.Core/Services.cs` e adicione-o à ordem de execução no `Simulation.Core/SimulationPipeline.cs`.