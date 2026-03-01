using NeneCM.Core.Models;
using Xunit;

namespace NeneCM.Core.Tests;

public class GameStateTests
{
    [Fact]
    public void DefaultState_HasCorrectInitialValues()
    {
        var state = new GameState();

        Assert.Equal(0, state.CurrentTurn);
        Assert.Equal(2024, state.CurrentYear);
        Assert.Equal(1, state.CurrentMonth);
        Assert.Empty(state.ActivePolicies);
        Assert.Empty(state.FactionApprovals);
        Assert.Equal(0, state.PlaytimeSeconds);
    }

    [Fact]
    public void CurrentDateString_FormatsCorrectly()
    {
        var state = new GameState { CurrentMonth = 6, CurrentYear = 2025 };

        Assert.Equal("Jun 2025", state.CurrentDateString);
    }

    [Fact]
    public void CurrentTermNumber_CalculatesCorrectly()
    {
        var state = new GameState { CurrentTurn = 0 };
        Assert.Equal(1, state.CurrentTermNumber);

        state.CurrentTurn = 59;
        Assert.Equal(1, state.CurrentTermNumber);

        state.CurrentTurn = 60;
        Assert.Equal(2, state.CurrentTermNumber);

        state.CurrentTurn = 120;
        Assert.Equal(3, state.CurrentTermNumber);
    }

    [Fact]
    public void TurnsUntilElection_CalculatesCorrectly()
    {
        var state = new GameState { CurrentTurn = 0 };
        Assert.Equal(60, state.TurnsUntilElection);

        state.CurrentTurn = 30;
        Assert.Equal(30, state.TurnsUntilElection);

        state.CurrentTurn = 59;
        Assert.Equal(1, state.TurnsUntilElection);

        state.CurrentTurn = 60;
        Assert.Equal(60, state.TurnsUntilElection);
    }

    [Fact]
    public void IsElectionTurn_ReturnsTrueOnlyAtElectionTurns()
    {
        var state = new GameState { CurrentTurn = 0 };
        Assert.False(state.IsElectionTurn);

        state.CurrentTurn = 59;
        Assert.False(state.IsElectionTurn);

        state.CurrentTurn = 60;
        Assert.True(state.IsElectionTurn);

        state.CurrentTurn = 61;
        Assert.False(state.IsElectionTurn);

        state.CurrentTurn = 120;
        Assert.True(state.IsElectionTurn);
    }

    [Fact]
    public void OverallApproval_ReturnsDefault_WhenNoFactions()
    {
        var state = new GameState();

        Assert.Equal(50f, state.OverallApproval);
    }

    [Fact]
    public void OverallApproval_CalculatesWeightedAverage()
    {
        var state = new GameState
        {
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 80, PopulationPercent = 50 },
                new() { FactionId = 2, Approval = 60, PopulationPercent = 50 }
            }
        };

        // (80 * 50 + 60 * 50) / 100 = 70
        Assert.Equal(70f, state.OverallApproval);
    }

    [Fact]
    public void OverallApproval_HandlesUnequalWeights()
    {
        var state = new GameState
        {
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 100, PopulationPercent = 75 },
                new() { FactionId = 2, Approval = 0, PopulationPercent = 25 }
            }
        };

        // (100 * 75 + 0 * 25) / 100 = 75
        Assert.Equal(75f, state.OverallApproval);
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        var original = new GameState
        {
            StateName = "Telangana",
            CurrentTurn = 30,
            CurrentYear = 2026,
            CurrentMonth = 7,
            Budget = new Budget { TotalBudget = 250000, AllocatedBudget = 50000 },
            Metrics = new Dictionary<string, float> { { "gdp_growth", 5.5f } },
            ActivePolicies = new List<ActivePolicy>
            {
                new() { PolicyId = 1, Level = 2, MonthsActive = 10 }
            },
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 65 }
            }
        };

        var clone = original.Clone();

        // Verify values are equal
        Assert.Equal(original.StateName, clone.StateName);
        Assert.Equal(original.CurrentTurn, clone.CurrentTurn);
        Assert.Equal(original.Budget.TotalBudget, clone.Budget.TotalBudget);
        Assert.Equal(original.Metrics["gdp_growth"], clone.Metrics["gdp_growth"]);

        // Verify it's a deep copy (modifications don't affect original)
        clone.CurrentTurn = 100;
        clone.Budget.TotalBudget = 500000;
        clone.Metrics["gdp_growth"] = 10f;
        clone.ActivePolicies[0].Level = 5;

        Assert.Equal(30, original.CurrentTurn);
        Assert.Equal(250000, original.Budget.TotalBudget);
        Assert.Equal(5.5f, original.Metrics["gdp_growth"]);
        Assert.Equal(2, original.ActivePolicies[0].Level);
    }

    [Fact]
    public void ToJson_And_FromJson_RoundTrip()
    {
        var original = new GameState
        {
            StateName = "Telangana",
            CurrentTurn = 45,
            CurrentYear = 2027,
            CurrentMonth = 10,
            Budget = new Budget { TotalBudget = 300000, AllocatedBudget = 75000, Debt = 10000 },
            Metrics = new Dictionary<string, float>
            {
                { "gdp_growth", 6.2f },
                { "unemployment", 4.5f }
            },
            PlaytimeSeconds = 3600,
            TermsWon = 1,
            IsPremium = true
        };

        var json = original.ToJson();
        var restored = GameState.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(original.StateName, restored.StateName);
        Assert.Equal(original.CurrentTurn, restored.CurrentTurn);
        Assert.Equal(original.CurrentYear, restored.CurrentYear);
        Assert.Equal(original.CurrentMonth, restored.CurrentMonth);
        Assert.Equal(original.Budget.TotalBudget, restored.Budget.TotalBudget);
        Assert.Equal(original.Budget.AllocatedBudget, restored.Budget.AllocatedBudget);
        Assert.Equal(original.Budget.Debt, restored.Budget.Debt);
        Assert.Equal(original.Metrics["gdp_growth"], restored.Metrics["gdp_growth"]);
        Assert.Equal(original.Metrics["unemployment"], restored.Metrics["unemployment"]);
        Assert.Equal(original.PlaytimeSeconds, restored.PlaytimeSeconds);
        Assert.Equal(original.TermsWon, restored.TermsWon);
        Assert.Equal(original.IsPremium, restored.IsPremium);
    }

    [Fact]
    public void FromJson_ThrowsForInvalidJson()
    {
        // JsonSerializer throws for invalid JSON
        Assert.Throws<System.Text.Json.JsonException>(() => GameState.FromJson("not valid json"));
    }

    [Fact]
    public void Budget_AvailableBudget_CalculatesCorrectly()
    {
        var budget = new Budget
        {
            TotalBudget = 250000,
            AllocatedBudget = 75000
        };

        Assert.Equal(175000, budget.AvailableBudget);
    }

    [Fact]
    public void Budget_IsInDeficit_WhenOverspent()
    {
        var budget = new Budget
        {
            TotalBudget = 100000,
            AllocatedBudget = 150000
        };

        Assert.True(budget.IsInDeficit);
        Assert.Equal(-50000, budget.Balance);
    }

    [Fact]
    public void Budget_Clone_CreatesDeepCopy()
    {
        var original = new Budget
        {
            TotalBudget = 200000,
            AllocatedBudget = 50000,
            Debt = 10000
        };

        var clone = original.Clone();
        clone.TotalBudget = 300000;

        Assert.Equal(200000, original.TotalBudget);
        Assert.Equal(300000, clone.TotalBudget);
    }
}
