using NeneCM.Core.Events;
using NeneCM.Core.Models;

namespace NeneCM.Core.Systems;

/// <summary>
/// Manages faction approval ratings and overall public opinion.
/// </summary>
public class ApprovalSystem
{
    private readonly EventBus _eventBus;

    /// <summary>
    /// Minimum approval rating.
    /// </summary>
    public const float MinApproval = 0f;

    /// <summary>
    /// Maximum approval rating.
    /// </summary>
    public const float MaxApproval = 100f;

    /// <summary>
    /// Natural decay per turn when no positive policies affect a faction.
    /// </summary>
    public const float NaturalDecayRate = 0.5f;

    /// <summary>
    /// Approval threshold needed to win election.
    /// </summary>
    public const float ElectionWinThreshold = 50f;

    public ApprovalSystem(EventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Modifies approval for a specific faction.
    /// </summary>
    public void ModifyFactionApproval(GameState state, int factionId, float delta)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        var faction = state.FactionApprovals.FirstOrDefault(f => f.FactionId == factionId);
        if (faction == null) return;

        float oldApproval = faction.Approval;
        faction.Approval = Math.Clamp(faction.Approval + delta, MinApproval, MaxApproval);

        if (Math.Abs(faction.Approval - oldApproval) > 0.01f)
        {
            _eventBus.Publish(new FactionApprovalChangedEvent
            {
                FactionId = factionId,
                OldApproval = oldApproval,
                NewApproval = faction.Approval
            });
        }
    }

    /// <summary>
    /// Modifies approval for all factions by category affinity.
    /// Factions with matching priorities get full effect, others get partial.
    /// </summary>
    public void ModifyApprovalByCategory(GameState state, string category, float delta, Dictionary<int, List<string>>? factionPriorities = null)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        foreach (var faction in state.FactionApprovals)
        {
            float effectiveDelta = delta;

            // If we have priority info, adjust based on faction interests
            if (factionPriorities != null && factionPriorities.TryGetValue(faction.FactionId, out var priorities))
            {
                if (priorities.Contains(category))
                {
                    effectiveDelta *= 1.5f; // Bonus for matching priority
                }
                else
                {
                    effectiveDelta *= 0.5f; // Reduced for non-priority
                }
            }

            ModifyFactionApproval(state, faction.FactionId, effectiveDelta);
        }
    }

    /// <summary>
    /// Applies natural approval decay.
    /// Called each turn.
    /// </summary>
    public void ApplyNaturalDecay(GameState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        foreach (var faction in state.FactionApprovals)
        {
            // Only decay if above neutral (50)
            if (faction.Approval > 50f)
            {
                ModifyFactionApproval(state, faction.FactionId, -NaturalDecayRate);
            }
        }
    }

    /// <summary>
    /// Calculates the weighted overall approval rating.
    /// </summary>
    public float CalculateOverallApproval(GameState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        return state.OverallApproval;
    }

    /// <summary>
    /// Determines if the player would win an election with current approval.
    /// </summary>
    public bool WouldWinElection(GameState state)
    {
        return CalculateOverallApproval(state) >= ElectionWinThreshold;
    }

    /// <summary>
    /// Gets the approval rating for a specific faction.
    /// </summary>
    public float GetFactionApproval(GameState state, int factionId)
    {
        var faction = state.FactionApprovals.FirstOrDefault(f => f.FactionId == factionId);
        return faction?.Approval ?? 50f;
    }

    /// <summary>
    /// Gets the faction with the lowest approval.
    /// </summary>
    public FactionApproval? GetLowestApprovalFaction(GameState state)
    {
        if (state == null || state.FactionApprovals.Count == 0) return null;
        return state.FactionApprovals.MinBy(f => f.Approval);
    }

    /// <summary>
    /// Gets the faction with the highest approval.
    /// </summary>
    public FactionApproval? GetHighestApprovalFaction(GameState state)
    {
        if (state == null || state.FactionApprovals.Count == 0) return null;
        return state.FactionApprovals.MaxBy(f => f.Approval);
    }

    /// <summary>
    /// Initializes default factions for a state.
    /// </summary>
    public static List<FactionApproval> CreateDefaultFactions()
    {
        return new List<FactionApproval>
        {
            new() { FactionId = 1, Approval = 50f, PopulationPercent = 35f }, // Rural farmers
            new() { FactionId = 2, Approval = 50f, PopulationPercent = 25f }, // Urban professionals
            new() { FactionId = 3, Approval = 50f, PopulationPercent = 15f }, // Students
            new() { FactionId = 4, Approval = 50f, PopulationPercent = 15f }, // Women
            new() { FactionId = 5, Approval = 50f, PopulationPercent = 10f }  // Business owners
        };
    }
}
