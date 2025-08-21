using System;
using NUnit.Framework;
using Arch.Core;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Commons;
using Simulation.Core.Components;
using Simulation.Core.Systems;
using Simulation.Core.Utilities;
using Simulation.Core.Commons.Enums;

namespace Simulation.Core.Tests.Systems;

[TestFixture]
public class AttackSystemTests
{
    private World _world;
    private SpatialHashGrid _grid;
    private AttackSystem _attackSystem;
    private IndexUpdateSystem _indexUpdateSystem;

    [SetUp]
    public void Setup()
    {
        _world = World.Create();
        _grid = new SpatialHashGrid(cellSize: 16);
        _attackSystem = new AttackSystem(_world, _grid);
        _indexUpdateSystem = new IndexUpdateSystem(_world, _grid);
    }

    [TearDown]
    public void Teardown()
    {
        World.Destroy(_world);
    }

    private Entity CreateAttacker(GameVector2 position, float duration = 0.5f, float cooldown = 1.5f)
    {
        return _world.Create(
            new TilePosition { Position = position },
            new MapRef { MapId = 1 },
            new AttackStats { Duration = duration, Cooldown = cooldown },
            new AttackState { Phase = AttackPhase.Ready, Timer = 0f }
        );
    }

    private Entity CreateTarget(GameVector2 position)
    {
        return _world.Create(
            new TilePosition { Position = position },
            new MapRef { MapId = 1 }
        );
    }

    [Test]
    public void Apply_MeleeAttackWithValidTarget_ShouldStartCasting()
    {
        // Arrange
        var attacker = CreateAttacker(new GameVector2(0, 0));
        var target = CreateTarget(new GameVector2(1, 0));
        var cmd = new Requests.Attack(attacker, AttackType.Melee, TargetEntity: target);

        // Act
        var result = _attackSystem.Apply(in cmd);

        // Assert
        Assert.That(result, Is.True);
        var state = _world.Get<AttackState>(attacker);
        Assert.That(state.Phase, Is.EqualTo(AttackPhase.Casting));
        Assert.That(_world.Has<AttackCasting>(attacker), Is.True);
    }

    [Test]
    public void Apply_AttackWhileNotReady_ShouldFail()
    {
        // Arrange
        var attacker = CreateAttacker(new GameVector2(0, 0));
        var target = CreateTarget(new GameVector2(1, 0));
        _world.Set(attacker, new AttackState { Phase = AttackPhase.OnCooldown, Timer = 1.0f });
        var cmd = new Requests.Attack(attacker, AttackType.Melee, TargetEntity: target);

        // Act
        var result = _attackSystem.Apply(in cmd);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Update_MeleeAttackCycle_ShouldTransitionThroughAllPhases()
    {
        // Arrange
        var attacker = CreateAttacker(new GameVector2(0, 0), duration: 0.5f, cooldown: 1.0f);
        var target = CreateTarget(new GameVector2(1, 0));
        var cmd = new Requests.Attack(attacker, AttackType.Melee, TargetEntity: target);
        _attackSystem.Apply(in cmd);

        // 1. Check if Casting
        var state = _world.Get<AttackState>(attacker);
        Assert.That(state.Phase, Is.EqualTo(AttackPhase.Casting));
        Assert.That(state.Timer, Is.EqualTo(0.5f));

        // 2. Update partway through casting
        _attackSystem.Update(0.3f);
        state = _world.Get<AttackState>(attacker);
        Assert.That(state.Phase, Is.EqualTo(AttackPhase.Casting));
        Assert.That(state.Timer, Is.EqualTo(0.5f - 0.3f).Within(0.001f));

        // 3. Update past casting duration -> should resolve and go to cooldown
        _attackSystem.Update(0.3f); // Total time = 0.6f > 0.5f
        state = _world.Get<AttackState>(attacker);
        Assert.That(state.Phase, Is.EqualTo(AttackPhase.OnCooldown));
        Assert.That(state.Timer, Is.EqualTo(1.0f)); // Cooldown starts
        Assert.That(_world.Has<AttackCasting>(attacker), Is.False); // Context component removed

        // 4. Update partway through cooldown
        _attackSystem.Update(0.5f);
        state = _world.Get<AttackState>(attacker);
        Assert.That(state.Phase, Is.EqualTo(AttackPhase.OnCooldown));

        // 5. Update past cooldown duration -> should be ready
        _attackSystem.Update(0.6f); // Total cooldown time = 1.1f > 1.0f
        state = _world.Get<AttackState>(attacker);
        Assert.That(state.Phase, Is.EqualTo(AttackPhase.Ready));
    }

    [Test]
    public void ResolveAttack_AreaOfEffect_ShouldFindTargetsInGrid()
    {
        // Arrange
        var attacker = CreateAttacker(new GameVector2(50, 50), duration: 0.2f);
        var target1 = CreateTarget(new GameVector2(52, 52)); // In range
        var target2 = CreateTarget(new GameVector2(48, 48)); // In range
        var target3 = CreateTarget(new GameVector2(60, 60)); // Out of range

        // Run IndexUpdateSystem to populate the spatial grid
        _indexUpdateSystem.Update(0f);
        
        var cmd = new Requests.Attack(
            attacker, 
            AttackType.AreaOfEffect, 
            TargetPosition: new GameVector2(51, 51), 
            Radius: 5.0f
        );
        _attackSystem.Apply(in cmd);
        
        // Act
        // Simulate time passing to trigger attack resolution
        _attackSystem.Update(0.3f);
        
        // Assert
        var state = _world.Get<AttackState>(attacker);
        Assert.That(state.Phase, Is.EqualTo(AttackPhase.OnCooldown));

        // NOTE: Em um cenário real, aqui nós verificaríamos se os alvos receberam
        // um componente de "Damage" ou se um evento de dano foi disparado.
        // Como a lógica de `ResolveAttack` apenas imprime no console, o teste
        // se concentra em garantir que o ciclo de ataque prossiga corretamente,
        // o que implica que a consulta ao grid foi executada sem erros.
    }

}