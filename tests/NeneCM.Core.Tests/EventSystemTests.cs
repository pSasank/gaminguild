using NeneCM.Core.Data;
using NeneCM.Core.Events;
using NeneCM.Core.Models;
using NeneCM.Core.Systems;
using Xunit;

namespace NeneCM.Core.Tests;

public class EventSystemTests
{
    private readonly EventBus _eventBus;
    private readonly List<EventDefinition> _testEvents;

    public EventSystemTests()
    {
        _eventBus = new EventBus();
        _testEvents = CreateTestEvents();
    }

    private List<EventDefinition> CreateTestEvents()
    {
        return new List<EventDefinition>
        {
            new EventDefinition
            {
                Id = 1,
                Name = "Test Event 1",
                Category = "test",
                Probability = 1.0f, // Always triggers
                Choices = new List<EventChoice>
                {
                    new() { Text = "Option A", Cost = 100, Effects = new List<EffectDefinition>
                        { new() { Metric = "gdp_growth", Value = 1f } } },
                    new() { Text = "Option B", Cost = 0, Effects = new List<EffectDefinition>() }
                }
            },
            new EventDefinition
            {
                Id = 2,
                Name = "Low Probability Event",
                Category = "test",
                Probability = 0.0f, // Never triggers
                Choices = new List<EventChoice>
                {
                    new() { Text = "Only Option", Cost = 0 }
                }
            },
            new EventDefinition
            {
                Id = 3,
                Name = "Conditional Event",
                Category = "test",
                Probability = 1.0f,
                Trigger = new Dictionary<string, object> { { "min_turn", 10 } },
                Choices = new List<EventChoice>
                {
                    new() { Text = "Accept", Cost = 500 }
                }
            }
        };
    }

    private GameState CreateTestState()
    {
        return new GameState
        {
            CurrentTurn = 5,
            CurrentMonth = 6,
            Budget = new Budget { TotalBudget = 100000, AllocatedBudget = 0 },
            Metrics = new Dictionary<string, float>
            {
                { "gdp_growth", 5f },
                { "infrastructure_index", 50f }
            },
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 50f, PopulationPercent = 100f }
            }
        };
    }

    [Fact]
    public void CheckForEvents_ReturnsEventWhenProbability100()
    {
        var events = new List<EventDefinition>
        {
            new() { Id = 1, Name = "Sure Event", Probability = 1.0f,
                Choices = new List<EventChoice> { new() { Text = "OK" } } }
        };
        var eventSystem = new EventSystem(_eventBus, events, seed: 42);
        var state = CreateTestState();

        var result = eventSystem.CheckForEvents(state);

        Assert.NotNull(result);
        Assert.Equal("Sure Event", result.Name);
    }

    [Fact]
    public void CheckForEvents_ReturnsNullWhenProbability0()
    {
        var events = new List<EventDefinition>
        {
            new() { Id = 1, Name = "Never Event", Probability = 0.0f,
                Choices = new List<EventChoice> { new() { Text = "OK" } } }
        };
        var eventSystem = new EventSystem(_eventBus, events, seed: 42);
        var state = CreateTestState();

        var result = eventSystem.CheckForEvents(state);

        Assert.Null(result);
    }

    [Fact]
    public void IsEventEligible_ReturnsTrueWhenNoConditions()
    {
        var eventSystem = new EventSystem(_eventBus, _testEvents);
        var state = CreateTestState();
        var eventDef = new EventDefinition { Id = 1, Trigger = null };

        var result = eventSystem.IsEventEligible(eventDef, state);

        Assert.True(result);
    }

    [Fact]
    public void IsEventEligible_ChecksMinTurnCondition()
    {
        var eventSystem = new EventSystem(_eventBus, _testEvents);
        var state = CreateTestState();
        state.CurrentTurn = 5;

        var eventDef = new EventDefinition
        {
            Id = 1,
            Trigger = new Dictionary<string, object> { { "min_turn", 10 } }
        };

        Assert.False(eventSystem.IsEventEligible(eventDef, state));

        state.CurrentTurn = 10;
        Assert.True(eventSystem.IsEventEligible(eventDef, state));
    }

    [Fact]
    public void ProcessEventChoice_AppliesCost()
    {
        var eventSystem = new EventSystem(_eventBus, _testEvents);
        var state = CreateTestState();
        var gameEvent = _testEvents[0]; // Has choice with cost 100

        eventSystem.ProcessEventChoice(state, gameEvent, 0);

        Assert.Equal(100, state.Budget.AllocatedBudget);
    }

    [Fact]
    public void ProcessEventChoice_AppliesMetricEffects()
    {
        var eventSystem = new EventSystem(_eventBus, _testEvents);
        var state = CreateTestState();
        state.Metrics["gdp_growth"] = 5f;
        var gameEvent = _testEvents[0]; // Choice 0 has gdp_growth +1

        eventSystem.ProcessEventChoice(state, gameEvent, 0);

        Assert.Equal(6f, state.Metrics["gdp_growth"]);
    }

    [Fact]
    public void ProcessEventChoice_AppliesFactionEffects()
    {
        var eventWithFaction = new EventDefinition
        {
            Id = 99,
            Choices = new List<EventChoice>
            {
                new()
                {
                    Text = "Help farmers",
                    Faction_effects = new List<FactionEffectDefinition>
                    {
                        new() { Faction_id = 1, Approval_change = 10f }
                    }
                }
            }
        };
        var eventSystem = new EventSystem(_eventBus, new List<EventDefinition> { eventWithFaction });
        var state = CreateTestState();

        eventSystem.ProcessEventChoice(state, eventWithFaction, 0);

        Assert.Equal(60f, state.FactionApprovals[0].Approval);
    }

    [Fact]
    public void ProcessEventChoice_ClampsApprovalTo100()
    {
        var eventWithBigBoost = new EventDefinition
        {
            Id = 99,
            Choices = new List<EventChoice>
            {
                new()
                {
                    Text = "Huge boost",
                    Faction_effects = new List<FactionEffectDefinition>
                    {
                        new() { Faction_id = 1, Approval_change = 100f }
                    }
                }
            }
        };
        var eventSystem = new EventSystem(_eventBus, new List<EventDefinition> { eventWithBigBoost });
        var state = CreateTestState();
        state.FactionApprovals[0].Approval = 90f;

        eventSystem.ProcessEventChoice(state, eventWithBigBoost, 0);

        Assert.Equal(100f, state.FactionApprovals[0].Approval);
    }

    [Fact]
    public void ProcessEventChoice_ThrowsForInvalidChoiceIndex()
    {
        var eventSystem = new EventSystem(_eventBus, _testEvents);
        var state = CreateTestState();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            eventSystem.ProcessEventChoice(state, _testEvents[0], 99));
    }

    [Fact]
    public void GetEligibleEvents_FiltersBasedOnConditions()
    {
        var eventSystem = new EventSystem(_eventBus, _testEvents);
        var state = CreateTestState();
        state.CurrentTurn = 5; // Below min_turn=10 for event 3

        var eligible = eventSystem.GetEligibleEvents(state);

        // Event 1 and 2 should be eligible (no conditions or met)
        // Event 3 has min_turn=10, so not eligible at turn 5
        Assert.Equal(2, eligible.Count);
        Assert.DoesNotContain(eligible, e => e.Id == 3);
    }

    [Fact]
    public void DeterministicSeed_ProducesSameResults()
    {
        var events = new List<EventDefinition>
        {
            new() { Id = 1, Name = "50/50 Event", Probability = 0.5f,
                Choices = new List<EventChoice> { new() { Text = "OK" } } }
        };

        var results1 = new List<bool>();
        var results2 = new List<bool>();

        var system1 = new EventSystem(_eventBus, events, seed: 12345);
        var system2 = new EventSystem(_eventBus, events, seed: 12345);

        for (int i = 0; i < 10; i++)
        {
            var state = CreateTestState();
            results1.Add(system1.CheckForEvents(state) != null);
            results2.Add(system2.CheckForEvents(state) != null);
        }

        Assert.Equal(results1, results2);
    }
}
