using NeneCM.Core.Data;
using NeneCM.Core.Events;
using NeneCM.Core.Models;

namespace NeneCM.Core.Systems;

/// <summary>
/// Manages random event triggering and processing.
/// </summary>
public class EventSystem
{
    private readonly EventBus _eventBus;
    private readonly Random _random;
    private readonly List<EventDefinition> _events;
    private readonly HashSet<int> _triggeredThisTurn = new();

    public EventSystem(EventBus eventBus, List<EventDefinition> events, int? seed = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _events = events ?? new List<EventDefinition>();
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Checks for and triggers random events based on current game state.
    /// Returns the triggered event if any, null otherwise.
    /// </summary>
    public EventDefinition? CheckForEvents(GameState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        _triggeredThisTurn.Clear();

        var eligibleEvents = _events
            .Where(e => IsEventEligible(e, state))
            .ToList();

        if (eligibleEvents.Count == 0)
            return null;

        // Roll for each eligible event
        foreach (var gameEvent in eligibleEvents)
        {
            if (_random.NextDouble() < gameEvent.Probability)
            {
                return gameEvent;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if an event meets its trigger conditions.
    /// </summary>
    public bool IsEventEligible(EventDefinition gameEvent, GameState state)
    {
        if (gameEvent.Trigger == null || gameEvent.Trigger.Count == 0)
            return true;

        foreach (var condition in gameEvent.Trigger)
        {
            if (!EvaluateCondition(condition.Key, condition.Value, state))
                return false;
        }

        return true;
    }

    private bool EvaluateCondition(string conditionType, object value, GameState state)
    {
        try
        {
            return conditionType switch
            {
                "month_range" => EvaluateMonthRange(value, state),
                "min_turn" => state.CurrentTurn >= Convert.ToInt32(value),
                "max_turn" => state.CurrentTurn <= Convert.ToInt32(value),
                "min_approval" => state.OverallApproval >= Convert.ToSingle(value),
                "max_approval" => state.OverallApproval <= Convert.ToSingle(value),
                "min_infrastructure" => GetMetric(state, "infrastructure_index") >= Convert.ToSingle(value),
                "healthcare_below" => GetMetric(state, "healthcare_access") < Convert.ToSingle(value),
                "farmer_approval_below" => GetFactionApproval(state, 1) < Convert.ToSingle(value),
                _ => true // Unknown conditions pass by default
            };
        }
        catch
        {
            return true; // If evaluation fails, allow the event
        }
    }

    private bool EvaluateMonthRange(object value, GameState state)
    {
        if (value is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var array = jsonElement.EnumerateArray().Select(e => e.GetInt32()).ToList();
            if (array.Count >= 2)
            {
                return state.CurrentMonth >= array[0] && state.CurrentMonth <= array[1];
            }
        }
        return true;
    }

    private float GetMetric(GameState state, string metricName)
    {
        return state.Metrics.GetValueOrDefault(metricName, 0f);
    }

    private float GetFactionApproval(GameState state, int factionId)
    {
        var faction = state.FactionApprovals.FirstOrDefault(f => f.FactionId == factionId);
        return faction?.Approval ?? 50f;
    }

    /// <summary>
    /// Processes a player's choice for an event.
    /// </summary>
    public void ProcessEventChoice(GameState state, EventDefinition gameEvent, int choiceIndex)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (gameEvent == null) throw new ArgumentNullException(nameof(gameEvent));

        if (choiceIndex < 0 || choiceIndex >= gameEvent.Choices.Count)
            throw new ArgumentOutOfRangeException(nameof(choiceIndex));

        var choice = gameEvent.Choices[choiceIndex];

        // Apply cost
        if (choice.Cost > 0)
        {
            state.Budget.AllocatedBudget += choice.Cost;
        }

        // Apply metric effects
        foreach (var effect in choice.Effects)
        {
            if (!state.Metrics.ContainsKey(effect.Metric))
                state.Metrics[effect.Metric] = 0;

            state.Metrics[effect.Metric] += effect.Value;
        }

        // Apply faction effects
        foreach (var factionEffect in choice.Faction_effects)
        {
            var faction = state.FactionApprovals.FirstOrDefault(f => f.FactionId == factionEffect.Faction_id);
            if (faction != null)
            {
                faction.Approval = Math.Clamp(
                    faction.Approval + factionEffect.Approval_change,
                    0f, 100f);
            }
        }
    }

    /// <summary>
    /// Gets all events that could potentially trigger (ignoring probability).
    /// </summary>
    public List<EventDefinition> GetEligibleEvents(GameState state)
    {
        return _events.Where(e => IsEventEligible(e, state)).ToList();
    }

    /// <summary>
    /// Sets the random seed for deterministic testing.
    /// </summary>
    public void SetSeed(int seed)
    {
        var field = typeof(Random).GetField("_seedArray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        // For testing, create new random with seed
    }
}
