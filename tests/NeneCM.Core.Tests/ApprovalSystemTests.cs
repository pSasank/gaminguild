using NeneCM.Core.Events;
using NeneCM.Core.Models;
using NeneCM.Core.Systems;
using Xunit;

namespace NeneCM.Core.Tests;

public class ApprovalSystemTests
{
    private readonly EventBus _eventBus;
    private readonly ApprovalSystem _approvalSystem;

    public ApprovalSystemTests()
    {
        _eventBus = new EventBus();
        _approvalSystem = new ApprovalSystem(_eventBus);
    }

    private GameState CreateTestState()
    {
        return new GameState
        {
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 50f, PopulationPercent = 40f },
                new() { FactionId = 2, Approval = 60f, PopulationPercent = 30f },
                new() { FactionId = 3, Approval = 70f, PopulationPercent = 30f }
            }
        };
    }

    [Fact]
    public void ModifyFactionApproval_ChangesApproval()
    {
        var state = CreateTestState();

        _approvalSystem.ModifyFactionApproval(state, 1, 10f);

        Assert.Equal(60f, state.FactionApprovals[0].Approval);
    }

    [Fact]
    public void ModifyFactionApproval_ClampsToMin()
    {
        var state = CreateTestState();

        _approvalSystem.ModifyFactionApproval(state, 1, -100f);

        Assert.Equal(0f, state.FactionApprovals[0].Approval);
    }

    [Fact]
    public void ModifyFactionApproval_ClampsToMax()
    {
        var state = CreateTestState();

        _approvalSystem.ModifyFactionApproval(state, 1, 100f);

        Assert.Equal(100f, state.FactionApprovals[0].Approval);
    }

    [Fact]
    public void ModifyFactionApproval_FiresEvent()
    {
        var state = CreateTestState();
        FactionApprovalChangedEvent? receivedEvent = null;
        _eventBus.Subscribe<FactionApprovalChangedEvent>(e => receivedEvent = e);

        _approvalSystem.ModifyFactionApproval(state, 1, 10f);

        Assert.NotNull(receivedEvent);
        Assert.Equal(1, receivedEvent.FactionId);
        Assert.Equal(50f, receivedEvent.OldApproval);
        Assert.Equal(60f, receivedEvent.NewApproval);
    }

    [Fact]
    public void ModifyFactionApproval_DoesNotFireEventForNoChange()
    {
        var state = CreateTestState();
        FactionApprovalChangedEvent? receivedEvent = null;
        _eventBus.Subscribe<FactionApprovalChangedEvent>(e => receivedEvent = e);

        _approvalSystem.ModifyFactionApproval(state, 1, 0f);

        Assert.Null(receivedEvent);
    }

    [Fact]
    public void ModifyApprovalByCategory_AffectsAllFactions()
    {
        var state = CreateTestState();

        _approvalSystem.ModifyApprovalByCategory(state, "agriculture", 10f);

        Assert.Equal(60f, state.FactionApprovals[0].Approval);
        Assert.Equal(70f, state.FactionApprovals[1].Approval);
        Assert.Equal(80f, state.FactionApprovals[2].Approval);
    }

    [Fact]
    public void ModifyApprovalByCategory_BonusForMatchingPriority()
    {
        var state = CreateTestState();
        var priorities = new Dictionary<int, List<string>>
        {
            { 1, new List<string> { "agriculture" } },
            { 2, new List<string> { "education" } },
            { 3, new List<string> { "agriculture" } }
        };

        _approvalSystem.ModifyApprovalByCategory(state, "agriculture", 10f, priorities);

        Assert.Equal(65f, state.FactionApprovals[0].Approval); // +15 (1.5x bonus)
        Assert.Equal(65f, state.FactionApprovals[1].Approval); // +5 (0.5x non-priority)
        Assert.Equal(85f, state.FactionApprovals[2].Approval); // +15 (1.5x bonus)
    }

    [Fact]
    public void ApplyNaturalDecay_DecaysAbove50()
    {
        var state = CreateTestState();
        state.FactionApprovals[0].Approval = 60f;

        _approvalSystem.ApplyNaturalDecay(state);

        Assert.Equal(59.5f, state.FactionApprovals[0].Approval);
    }

    [Fact]
    public void ApplyNaturalDecay_DoesNotDecayAt50OrBelow()
    {
        var state = CreateTestState();
        state.FactionApprovals[0].Approval = 50f;
        state.FactionApprovals[1].Approval = 40f;

        _approvalSystem.ApplyNaturalDecay(state);

        Assert.Equal(50f, state.FactionApprovals[0].Approval);
        Assert.Equal(40f, state.FactionApprovals[1].Approval);
    }

    [Fact]
    public void CalculateOverallApproval_ReturnsWeightedAverage()
    {
        var state = CreateTestState();
        // Faction 1: 50 * 40% = 20
        // Faction 2: 60 * 30% = 18
        // Faction 3: 70 * 30% = 21
        // Total: 59

        float approval = _approvalSystem.CalculateOverallApproval(state);

        Assert.Equal(59f, approval);
    }

    [Fact]
    public void WouldWinElection_TrueAt50OrAbove()
    {
        var state = CreateTestState();
        state.FactionApprovals = new List<FactionApproval>
        {
            new() { FactionId = 1, Approval = 50f, PopulationPercent = 100f }
        };

        bool result = _approvalSystem.WouldWinElection(state);

        Assert.True(result);
    }

    [Fact]
    public void WouldWinElection_FalseBelow50()
    {
        var state = CreateTestState();
        state.FactionApprovals = new List<FactionApproval>
        {
            new() { FactionId = 1, Approval = 49f, PopulationPercent = 100f }
        };

        bool result = _approvalSystem.WouldWinElection(state);

        Assert.False(result);
    }

    [Fact]
    public void GetFactionApproval_ReturnsCorrectValue()
    {
        var state = CreateTestState();

        float approval = _approvalSystem.GetFactionApproval(state, 2);

        Assert.Equal(60f, approval);
    }

    [Fact]
    public void GetFactionApproval_ReturnsDefaultForUnknownFaction()
    {
        var state = CreateTestState();

        float approval = _approvalSystem.GetFactionApproval(state, 999);

        Assert.Equal(50f, approval);
    }

    [Fact]
    public void GetLowestApprovalFaction_ReturnsCorrectFaction()
    {
        var state = CreateTestState();

        var faction = _approvalSystem.GetLowestApprovalFaction(state);

        Assert.NotNull(faction);
        Assert.Equal(1, faction.FactionId);
        Assert.Equal(50f, faction.Approval);
    }

    [Fact]
    public void GetHighestApprovalFaction_ReturnsCorrectFaction()
    {
        var state = CreateTestState();

        var faction = _approvalSystem.GetHighestApprovalFaction(state);

        Assert.NotNull(faction);
        Assert.Equal(3, faction.FactionId);
        Assert.Equal(70f, faction.Approval);
    }

    [Fact]
    public void CreateDefaultFactions_ReturnsValidFactions()
    {
        var factions = ApprovalSystem.CreateDefaultFactions();

        Assert.Equal(5, factions.Count);
        Assert.All(factions, f => Assert.Equal(50f, f.Approval));

        float totalPercent = factions.Sum(f => f.PopulationPercent);
        Assert.Equal(100f, totalPercent);
    }
}
