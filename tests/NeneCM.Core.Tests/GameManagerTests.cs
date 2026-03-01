using NeneCM.Core;
using NeneCM.Core.Data;
using NeneCM.Core.Events;
using NeneCM.Core.Models;
using NeneCM.Core.Tests.Mocks;
using Xunit;

namespace NeneCM.Core.Tests;

public class GameManagerTests
{
    private readonly MockPolicyRepository _repository;
    private readonly StateContent _content;

    public GameManagerTests()
    {
        _repository = new MockPolicyRepository();
        _content = CreateTestContent();
    }

    private StateContent CreateTestContent()
    {
        return new StateContent
        {
            State = new StateInfo
            {
                Name = "TestState",
                Starting_budget = 100000,
                Starting_year = 2024
            },
            Metrics = new List<MetricDefinition>
            {
                new() { Name = "gdp_growth", Base_value = 5f, Min = -10, Max = 20 },
                new() { Name = "unemployment", Base_value = 5f, Min = 0, Max = 30 },
                new() { Name = "approval_rural", Base_value = 50f, Min = 0, Max = 100 }
            },
            Factions = new List<FactionDefinition>
            {
                new() { Id = 1, Name = "Rural", Population_percent = 50, Priorities = new List<string> { "agriculture" } },
                new() { Id = 2, Name = "Urban", Population_percent = 50, Priorities = new List<string> { "industry" } }
            },
            Policies = new List<PolicyDefinition>(),
            Events = new List<EventDefinition>()
        };
    }

    [Fact]
    public void CreateNewGame_InitializesCorrectly()
    {
        var manager = new GameManager(_repository, _content);

        Assert.NotNull(manager.CurrentState);
        Assert.Equal("TestState", manager.CurrentState.StateName);
        Assert.Equal(0, manager.CurrentState.CurrentTurn);
        Assert.Equal(2024, manager.CurrentState.CurrentYear);
        Assert.Equal(100000, manager.CurrentState.Budget.TotalBudget);
    }

    [Fact]
    public void CreateNewGame_InitializesMetrics()
    {
        var manager = new GameManager(_repository, _content);

        Assert.Equal(5f, manager.CurrentState.Metrics["gdp_growth"]);
        Assert.Equal(5f, manager.CurrentState.Metrics["unemployment"]);
    }

    [Fact]
    public void CreateNewGame_InitializesFactions()
    {
        var manager = new GameManager(_repository, _content);

        Assert.Equal(2, manager.CurrentState.FactionApprovals.Count);
        Assert.All(manager.CurrentState.FactionApprovals, f => Assert.Equal(50f, f.Approval));
    }

    [Fact]
    public void AdvanceTurn_IncrementsTurn()
    {
        var manager = new GameManager(_repository, _content, randomSeed: 42);

        var result = manager.AdvanceTurn();

        Assert.True(result.Success);
        Assert.Equal(1, manager.CurrentState.CurrentTurn);
    }

    [Fact]
    public void AdvanceTurn_ReturnsNewTurnInResult()
    {
        var manager = new GameManager(_repository, _content, randomSeed: 42);

        var result = manager.AdvanceTurn();

        Assert.Equal(1, result.NewTurn);
    }

    [Fact]
    public void ImplementPolicy_Works()
    {
        var manager = new GameManager(_repository, _content);

        var result = manager.ImplementPolicy(1); // Policy from mock repository

        Assert.True(result);
        Assert.Single(manager.CurrentState.ActivePolicies);
    }

    [Fact]
    public void ImplementPolicy_FailsWhenGameOver()
    {
        var manager = new GameManager(_repository, _content);
        // Simulate game over by advancing to election and losing
        manager.CurrentState.FactionApprovals.ForEach(f => f.Approval = 10f);
        manager.CurrentState.CurrentTurn = 59;
        manager.AdvanceTurn(); // This triggers election loss

        var result = manager.ImplementPolicy(1);

        Assert.False(result);
    }

    [Fact]
    public void RemovePolicy_Works()
    {
        var manager = new GameManager(_repository, _content);
        manager.ImplementPolicy(1);

        var result = manager.RemovePolicy(1);

        Assert.True(result);
        Assert.Empty(manager.CurrentState.ActivePolicies);
    }

    [Fact]
    public void UpgradePolicy_Works()
    {
        var manager = new GameManager(_repository, _content);
        manager.ImplementPolicy(1, level: 1);

        var result = manager.UpgradePolicy(1);

        Assert.True(result);
        Assert.Equal(2, manager.CurrentState.ActivePolicies[0].Level);
    }

    [Fact]
    public void GetAvailablePolicies_ReturnsPolicies()
    {
        var manager = new GameManager(_repository, _content);

        var policies = manager.GetAvailablePolicies().ToList();

        Assert.NotEmpty(policies);
    }

    [Fact]
    public void GetAvailablePolicies_FiltersByCategory()
    {
        var manager = new GameManager(_repository, _content);

        var policies = manager.GetAvailablePolicies("agriculture").ToList();

        Assert.All(policies, p => Assert.Equal("agriculture", p.Category));
    }

    [Fact]
    public void LoadState_RestoresState()
    {
        var manager = new GameManager(_repository, _content);
        var savedState = new GameState
        {
            StateName = "LoadedState",
            CurrentTurn = 30,
            CurrentYear = 2026
        };

        manager.LoadState(savedState);

        Assert.Equal("LoadedState", manager.CurrentState.StateName);
        Assert.Equal(30, manager.CurrentState.CurrentTurn);
    }

    [Fact]
    public void LoadState_ClearsGameOver()
    {
        var manager = new GameManager(_repository, _content);
        manager.CurrentState.FactionApprovals.ForEach(f => f.Approval = 10f);
        manager.CurrentState.CurrentTurn = 59;
        manager.AdvanceTurn(); // Game over
        Assert.True(manager.IsGameOver);

        manager.LoadState(new GameState { StateName = "Fresh" });

        Assert.False(manager.IsGameOver);
    }

    [Fact]
    public void EventBus_IsAccessible()
    {
        var manager = new GameManager(_repository, _content);

        var eventBus = manager.GetEventBus();

        Assert.NotNull(eventBus);
    }

    [Fact]
    public void ElectionLoss_SetsGameOver()
    {
        var manager = new GameManager(_repository, _content, randomSeed: 42);
        manager.CurrentState.FactionApprovals.ForEach(f => f.Approval = 30f);
        manager.CurrentState.CurrentTurn = 59;

        manager.AdvanceTurn();

        Assert.True(manager.IsGameOver);
        Assert.Equal("Lost election", manager.GameOverReason);
    }

    [Fact]
    public void ElectionWin_ContinuesGame()
    {
        var manager = new GameManager(_repository, _content, randomSeed: 42);
        manager.CurrentState.FactionApprovals.ForEach(f => f.Approval = 70f);
        manager.CurrentState.CurrentTurn = 59;

        manager.AdvanceTurn();

        Assert.False(manager.IsGameOver);
        Assert.Equal(1, manager.CurrentState.TermsWon);
    }

    [Fact]
    public void PendingEvent_BlocksTurnAdvance()
    {
        var contentWithEvent = CreateTestContent();
        contentWithEvent.Events.Add(new EventDefinition
        {
            Id = 1,
            Name = "Blocking Event",
            Probability = 1.0f,
            Choices = new List<EventChoice> { new() { Text = "OK" } }
        });
        var manager = new GameManager(_repository, contentWithEvent, randomSeed: 42);

        manager.AdvanceTurn(); // Triggers event
        Assert.NotNull(manager.PendingEvent);

        var result = manager.AdvanceTurn(); // Should fail

        Assert.False(result.Success);
        Assert.Contains("pending event", result.Message.ToLower());
    }

    [Fact]
    public void ResolveEvent_ClearsPendingEvent()
    {
        var contentWithEvent = CreateTestContent();
        contentWithEvent.Events.Add(new EventDefinition
        {
            Id = 1,
            Name = "Test Event",
            Probability = 1.0f,
            Choices = new List<EventChoice> { new() { Text = "OK" } }
        });
        var manager = new GameManager(_repository, contentWithEvent, randomSeed: 42);
        manager.AdvanceTurn();
        Assert.NotNull(manager.PendingEvent);

        var result = manager.ResolveEvent(0);

        Assert.True(result);
        Assert.Null(manager.PendingEvent);
    }

    [Fact]
    public void MultiTurnSimulation_WorksCorrectly()
    {
        var manager = new GameManager(_repository, _content, randomSeed: 42);
        manager.CurrentState.FactionApprovals.ForEach(f => f.Approval = 60f);

        // Advance 12 turns (1 year)
        for (int i = 0; i < 12; i++)
        {
            var result = manager.AdvanceTurn();
            Assert.True(result.Success);
        }

        Assert.Equal(12, manager.CurrentState.CurrentTurn);
        Assert.Equal(2025, manager.CurrentState.CurrentYear);
        Assert.Equal(1, manager.CurrentState.CurrentMonth);
    }
}
