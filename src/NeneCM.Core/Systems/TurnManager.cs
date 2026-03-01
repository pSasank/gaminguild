using NeneCM.Core.Events;
using NeneCM.Core.Models;

namespace NeneCM.Core.Systems;

/// <summary>
/// Manages the turn-based game loop.
/// Each turn represents one month of governance.
/// </summary>
public class TurnManager
{
    private readonly EventBus _eventBus;

    /// <summary>
    /// Number of turns per election cycle (5 years = 60 months).
    /// </summary>
    public const int TurnsPerTerm = 60;

    /// <summary>
    /// Months in a year.
    /// </summary>
    public const int MonthsPerYear = 12;

    public TurnManager(EventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Advances the game by one turn (one month).
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <returns>True if turn was advanced successfully.</returns>
    public bool AdvanceTurn(GameState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        // Increment turn counter
        state.CurrentTurn++;

        // Advance month
        state.CurrentMonth++;

        // Handle year rollover
        if (state.CurrentMonth > MonthsPerYear)
        {
            state.CurrentMonth = 1;
            state.CurrentYear++;
        }

        // Increment months active for all policies
        foreach (var policy in state.ActivePolicies)
        {
            policy.MonthsActive++;
        }

        // Publish turn advanced event
        _eventBus.Publish(new TurnAdvancedEvent
        {
            NewTurn = state.CurrentTurn,
            NewMonth = state.CurrentMonth,
            NewYear = state.CurrentYear
        });

        // Check for election
        if (state.IsElectionTurn)
        {
            TriggerElection(state);
        }

        return true;
    }

    /// <summary>
    /// Triggers an election and determines the result.
    /// </summary>
    /// <param name="state">The current game state.</param>
    private void TriggerElection(GameState state)
    {
        var approval = state.OverallApproval;
        var termNumber = state.CurrentTermNumber;

        // Publish election triggered event
        _eventBus.Publish(new ElectionTriggeredEvent
        {
            OverallApproval = approval,
            TermNumber = termNumber
        });

        // Determine result (50% approval needed to win)
        bool won = approval >= 50f;

        if (won)
        {
            state.TermsWon++;
        }

        // Publish result
        _eventBus.Publish(new ElectionResultEvent
        {
            Won = won,
            Approval = approval,
            TermNumber = termNumber
        });

        // Handle game over for free users who lose, or any loss
        if (!won)
        {
            _eventBus.Publish(new GameOverEvent
            {
                Reason = "Lost election",
                FinalTurn = state.CurrentTurn,
                FinalApproval = approval
            });
        }
        // Check if free user completed Term 1
        else if (termNumber == 1 && !state.IsPremium)
        {
            // This is where we'd show upgrade prompt
            // But we still let them continue for now
        }
    }

    /// <summary>
    /// Gets the number of turns until the next election.
    /// </summary>
    public static int GetTurnsUntilElection(GameState state)
    {
        return TurnsPerTerm - (state.CurrentTurn % TurnsPerTerm);
    }

    /// <summary>
    /// Gets whether an election should occur this turn.
    /// </summary>
    public static bool IsElectionTurn(int turn)
    {
        return turn > 0 && turn % TurnsPerTerm == 0;
    }

    /// <summary>
    /// Calculates the date (month, year) for a given turn number,
    /// starting from an initial date.
    /// </summary>
    public static (int month, int year) GetDateForTurn(int turn, int startMonth = 1, int startYear = 2024)
    {
        int totalMonths = startMonth - 1 + turn; // Convert to 0-indexed months
        int year = startYear + (totalMonths / MonthsPerYear);
        int month = (totalMonths % MonthsPerYear) + 1; // Back to 1-indexed
        return (month, year);
    }
}
