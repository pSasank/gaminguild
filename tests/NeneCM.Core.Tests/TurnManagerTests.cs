using NeneCM.Core.Events;
using NeneCM.Core.Models;
using NeneCM.Core.Systems;
using Xunit;

namespace NeneCM.Core.Tests;

public class TurnManagerTests
{
    private readonly EventBus _eventBus;
    private readonly TurnManager _turnManager;

    public TurnManagerTests()
    {
        _eventBus = new EventBus();
        _turnManager = new TurnManager(_eventBus);
    }

    [Fact]
    public void AdvanceTurn_IncrementsTurnCounter()
    {
        var state = new GameState { CurrentTurn = 0 };

        _turnManager.AdvanceTurn(state);

        Assert.Equal(1, state.CurrentTurn);
    }

    [Fact]
    public void AdvanceTurn_IncrementsMonth()
    {
        var state = new GameState { CurrentMonth = 1 };

        _turnManager.AdvanceTurn(state);

        Assert.Equal(2, state.CurrentMonth);
    }

    [Fact]
    public void AdvanceTurn_HandlesYearRollover()
    {
        var state = new GameState { CurrentMonth = 12, CurrentYear = 2024 };

        _turnManager.AdvanceTurn(state);

        Assert.Equal(1, state.CurrentMonth);
        Assert.Equal(2025, state.CurrentYear);
    }

    [Fact]
    public void AdvanceTurn_IncrementsMonthsActiveForPolicies()
    {
        var state = new GameState
        {
            ActivePolicies = new List<ActivePolicy>
            {
                new() { PolicyId = 1, MonthsActive = 0 },
                new() { PolicyId = 2, MonthsActive = 5 }
            }
        };

        _turnManager.AdvanceTurn(state);

        Assert.Equal(1, state.ActivePolicies[0].MonthsActive);
        Assert.Equal(6, state.ActivePolicies[1].MonthsActive);
    }

    [Fact]
    public void AdvanceTurn_FiresTurnAdvancedEvent()
    {
        var state = new GameState { CurrentTurn = 5, CurrentMonth = 6, CurrentYear = 2024 };
        TurnAdvancedEvent? receivedEvent = null;
        _eventBus.Subscribe<TurnAdvancedEvent>(e => receivedEvent = e);

        _turnManager.AdvanceTurn(state);

        Assert.NotNull(receivedEvent);
        Assert.Equal(6, receivedEvent.NewTurn);
        Assert.Equal(7, receivedEvent.NewMonth);
        Assert.Equal(2024, receivedEvent.NewYear);
    }

    [Fact]
    public void AdvanceTurn_AtTurn60_TriggersElection()
    {
        var state = new GameState
        {
            CurrentTurn = 59,
            CurrentMonth = 12,
            CurrentYear = 2028,
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 60, PopulationPercent = 100 }
            }
        };
        ElectionTriggeredEvent? electionEvent = null;
        _eventBus.Subscribe<ElectionTriggeredEvent>(e => electionEvent = e);

        _turnManager.AdvanceTurn(state);

        Assert.NotNull(electionEvent);
        Assert.Equal(60f, electionEvent.OverallApproval);
        Assert.Equal(2, electionEvent.TermNumber); // Turn 60 = start of term 2
    }

    [Fact]
    public void AdvanceTurn_ElectionWon_IncrementsTermsWon()
    {
        var state = new GameState
        {
            CurrentTurn = 59,
            TermsWon = 0,
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 55, PopulationPercent = 100 }
            }
        };
        ElectionResultEvent? resultEvent = null;
        _eventBus.Subscribe<ElectionResultEvent>(e => resultEvent = e);

        _turnManager.AdvanceTurn(state);

        Assert.NotNull(resultEvent);
        Assert.True(resultEvent.Won);
        Assert.Equal(1, state.TermsWon);
    }

    [Fact]
    public void AdvanceTurn_ElectionLost_FiresGameOverEvent()
    {
        var state = new GameState
        {
            CurrentTurn = 59,
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 45, PopulationPercent = 100 }
            }
        };
        GameOverEvent? gameOverEvent = null;
        _eventBus.Subscribe<GameOverEvent>(e => gameOverEvent = e);

        _turnManager.AdvanceTurn(state);

        Assert.NotNull(gameOverEvent);
        Assert.Equal("Lost election", gameOverEvent.Reason);
        Assert.Equal(60, gameOverEvent.FinalTurn);
        Assert.Equal(45f, gameOverEvent.FinalApproval);
    }

    [Fact]
    public void AdvanceTurn_Exactly50PercentApproval_WinsElection()
    {
        var state = new GameState
        {
            CurrentTurn = 59,
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 50, PopulationPercent = 100 }
            }
        };
        ElectionResultEvent? resultEvent = null;
        _eventBus.Subscribe<ElectionResultEvent>(e => resultEvent = e);

        _turnManager.AdvanceTurn(state);

        Assert.NotNull(resultEvent);
        Assert.True(resultEvent.Won);
    }

    [Fact]
    public void AdvanceTurn_JustBelow50Percent_LosesElection()
    {
        var state = new GameState
        {
            CurrentTurn = 59,
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 49.9f, PopulationPercent = 100 }
            }
        };
        ElectionResultEvent? resultEvent = null;
        _eventBus.Subscribe<ElectionResultEvent>(e => resultEvent = e);

        _turnManager.AdvanceTurn(state);

        Assert.NotNull(resultEvent);
        Assert.False(resultEvent.Won);
    }

    [Fact]
    public void AdvanceTurn_ThrowsOnNullState()
    {
        Assert.Throws<ArgumentNullException>(() => _turnManager.AdvanceTurn(null!));
    }

    [Fact]
    public void GetTurnsUntilElection_CalculatesCorrectly()
    {
        var state = new GameState { CurrentTurn = 0 };
        Assert.Equal(60, TurnManager.GetTurnsUntilElection(state));

        state.CurrentTurn = 30;
        Assert.Equal(30, TurnManager.GetTurnsUntilElection(state));

        state.CurrentTurn = 59;
        Assert.Equal(1, TurnManager.GetTurnsUntilElection(state));

        state.CurrentTurn = 60;
        Assert.Equal(60, TurnManager.GetTurnsUntilElection(state));
    }

    [Fact]
    public void IsElectionTurn_Static_CalculatesCorrectly()
    {
        Assert.False(TurnManager.IsElectionTurn(0));
        Assert.False(TurnManager.IsElectionTurn(59));
        Assert.True(TurnManager.IsElectionTurn(60));
        Assert.False(TurnManager.IsElectionTurn(61));
        Assert.True(TurnManager.IsElectionTurn(120));
        Assert.True(TurnManager.IsElectionTurn(180));
    }

    [Fact]
    public void GetDateForTurn_CalculatesCorrectly()
    {
        // Starting Jan 2024
        Assert.Equal((1, 2024), TurnManager.GetDateForTurn(0));
        Assert.Equal((2, 2024), TurnManager.GetDateForTurn(1));
        Assert.Equal((12, 2024), TurnManager.GetDateForTurn(11));
        Assert.Equal((1, 2025), TurnManager.GetDateForTurn(12));
        Assert.Equal((1, 2029), TurnManager.GetDateForTurn(60)); // 5 years later
    }

    [Fact]
    public void GetDateForTurn_WithCustomStartDate()
    {
        // Starting June 2023
        Assert.Equal((6, 2023), TurnManager.GetDateForTurn(0, startMonth: 6, startYear: 2023));
        Assert.Equal((7, 2023), TurnManager.GetDateForTurn(1, startMonth: 6, startYear: 2023));
        Assert.Equal((1, 2024), TurnManager.GetDateForTurn(7, startMonth: 6, startYear: 2023));
    }

    [Fact]
    public void AdvanceTurn_MultipleTurns_TracksCorrectly()
    {
        var state = new GameState
        {
            CurrentTurn = 0,
            CurrentMonth = 1,
            CurrentYear = 2024,
            FactionApprovals = new List<FactionApproval>
            {
                new() { FactionId = 1, Approval = 60, PopulationPercent = 100 }
            }
        };

        // Advance 24 turns (2 years)
        for (int i = 0; i < 24; i++)
        {
            _turnManager.AdvanceTurn(state);
        }

        Assert.Equal(24, state.CurrentTurn);
        Assert.Equal(1, state.CurrentMonth);
        Assert.Equal(2026, state.CurrentYear);
    }

    [Fact]
    public void Constructor_ThrowsOnNullEventBus()
    {
        Assert.Throws<ArgumentNullException>(() => new TurnManager(null!));
    }
}
