using NeneCM.Core.Data;
using NeneCM.Core.Events;
using NeneCM.Core.Interfaces;
using NeneCM.Core.Models;
using NeneCM.Core.Systems;

namespace NeneCM.Core;

/// <summary>
/// Main game orchestrator that coordinates all systems.
/// </summary>
public class GameManager
{
    private readonly EventBus _eventBus;
    private readonly TurnManager _turnManager;
    private readonly PolicySystem _policySystem;
    private readonly BudgetSystem _budgetSystem;
    private readonly ApprovalSystem _approvalSystem;
    private readonly EventSystem _eventSystem;
    private readonly StateContent _stateContent;

    public GameState CurrentState { get; private set; }
    public EventDefinition? PendingEvent { get; private set; }
    public bool IsGameOver { get; private set; }
    public string GameOverReason { get; private set; } = string.Empty;

    public GameManager(
        IPolicyRepository policyRepository,
        StateContent stateContent,
        int? randomSeed = null)
    {
        _stateContent = stateContent ?? throw new ArgumentNullException(nameof(stateContent));

        _eventBus = new EventBus();
        _turnManager = new TurnManager(_eventBus);
        _policySystem = new PolicySystem(policyRepository, _eventBus);
        _budgetSystem = new BudgetSystem(_eventBus);
        _approvalSystem = new ApprovalSystem(_eventBus);
        _eventSystem = new EventSystem(_eventBus, stateContent.Events, randomSeed);

        CurrentState = CreateNewGame();

        // Subscribe to game events
        _eventBus.Subscribe<GameOverEvent>(OnGameOver);
        _eventBus.Subscribe<ElectionResultEvent>(OnElectionResult);
    }

    /// <summary>
    /// Creates a new game state from the loaded content.
    /// </summary>
    public GameState CreateNewGame()
    {
        var state = new GameState
        {
            StateName = _stateContent.State.Name,
            CurrentTurn = 0,
            CurrentYear = _stateContent.State.Starting_year,
            CurrentMonth = 1,
            Budget = new Budget
            {
                TotalBudget = _stateContent.State.Starting_budget,
                AllocatedBudget = 0,
                Debt = 0
            }
        };

        // Initialize metrics
        foreach (var metric in _stateContent.Metrics)
        {
            state.Metrics[metric.Name] = metric.Base_value;
        }

        // Initialize factions
        foreach (var faction in _stateContent.Factions)
        {
            state.FactionApprovals.Add(new FactionApproval
            {
                FactionId = faction.Id,
                Approval = 50f,
                PopulationPercent = faction.Population_percent
            });
        }

        return state;
    }

    /// <summary>
    /// Advances the game by one turn.
    /// </summary>
    public TurnResult AdvanceTurn()
    {
        if (IsGameOver)
            return new TurnResult { Success = false, Message = "Game is over" };

        if (PendingEvent != null)
            return new TurnResult { Success = false, Message = "Must resolve pending event first" };

        // Process yearly budget at start of year
        if (CurrentState.CurrentMonth == 1 && CurrentState.CurrentTurn > 0)
        {
            _budgetSystem.ProcessYearlyBudget(CurrentState, _stateContent.State.Starting_budget);
        }

        // Apply policy effects
        _policySystem.ApplyPolicyEffects(CurrentState);

        // Process monthly budget
        _budgetSystem.ProcessMonthlyBudget(CurrentState);

        // Apply natural approval decay
        _approvalSystem.ApplyNaturalDecay(CurrentState);

        // Check for bankruptcy
        if (_budgetSystem.IsBankrupt(CurrentState))
        {
            IsGameOver = true;
            GameOverReason = "Bankruptcy";
            _eventBus.Publish(new GameOverEvent
            {
                Reason = "Bankruptcy",
                FinalTurn = CurrentState.CurrentTurn,
                FinalApproval = CurrentState.OverallApproval
            });
            return new TurnResult { Success = true, IsGameOver = true, Message = "Bankrupt!" };
        }

        // Advance turn (handles elections)
        _turnManager.AdvanceTurn(CurrentState);

        // Check for random events
        PendingEvent = _eventSystem.CheckForEvents(CurrentState);

        return new TurnResult
        {
            Success = true,
            NewTurn = CurrentState.CurrentTurn,
            HasEvent = PendingEvent != null,
            Event = PendingEvent,
            IsElectionTurn = CurrentState.IsElectionTurn,
            IsGameOver = IsGameOver
        };
    }

    /// <summary>
    /// Implements a policy.
    /// </summary>
    public bool ImplementPolicy(int policyId, int level = 1)
    {
        if (IsGameOver) return false;
        return _policySystem.ImplementPolicy(CurrentState, policyId, level);
    }

    /// <summary>
    /// Removes a policy.
    /// </summary>
    public bool RemovePolicy(int policyId)
    {
        if (IsGameOver) return false;
        return _policySystem.RemovePolicy(CurrentState, policyId);
    }

    /// <summary>
    /// Upgrades a policy.
    /// </summary>
    public bool UpgradePolicy(int policyId)
    {
        if (IsGameOver) return false;
        return _policySystem.UpgradePolicy(CurrentState, policyId);
    }

    /// <summary>
    /// Resolves a pending event with the chosen option.
    /// </summary>
    public bool ResolveEvent(int choiceIndex)
    {
        if (PendingEvent == null)
            return false;

        _eventSystem.ProcessEventChoice(CurrentState, PendingEvent, choiceIndex);
        PendingEvent = null;
        return true;
    }

    /// <summary>
    /// Gets all available policies.
    /// </summary>
    public IEnumerable<Policy> GetAvailablePolicies(string? category = null)
    {
        return _policySystem.GetAvailablePolicies(category);
    }

    /// <summary>
    /// Gets the event bus for subscribing to game events.
    /// </summary>
    public EventBus GetEventBus() => _eventBus;

    /// <summary>
    /// Loads a saved game state.
    /// </summary>
    public void LoadState(GameState state)
    {
        CurrentState = state ?? throw new ArgumentNullException(nameof(state));
        IsGameOver = false;
        GameOverReason = string.Empty;
        PendingEvent = null;
    }

    private void OnGameOver(GameOverEvent e)
    {
        IsGameOver = true;
        GameOverReason = e.Reason;
    }

    private void OnElectionResult(ElectionResultEvent e)
    {
        if (!e.Won)
        {
            IsGameOver = true;
            GameOverReason = "Lost election";
        }
    }
}

/// <summary>
/// Result of advancing a turn.
/// </summary>
public class TurnResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int NewTurn { get; init; }
    public bool HasEvent { get; init; }
    public EventDefinition? Event { get; init; }
    public bool IsElectionTurn { get; init; }
    public bool IsGameOver { get; init; }
}
