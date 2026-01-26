using NeneCM.Core.Events;
using NeneCM.Core.Interfaces;
using NeneCM.Core.Models;

namespace NeneCM.Core.Systems;

/// <summary>
/// Manages policy implementation, upgrades, and effect application.
/// </summary>
public class PolicySystem
{
    private readonly IPolicyRepository _repository;
    private readonly EventBus _eventBus;

    public PolicySystem(IPolicyRepository repository, EventBus eventBus)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Gets all available policies.
    /// </summary>
    public IEnumerable<Policy> GetAvailablePolicies(string? category = null)
    {
        return string.IsNullOrEmpty(category)
            ? _repository.GetAllPolicies()
            : _repository.GetPoliciesByCategory(category);
    }

    /// <summary>
    /// Attempts to implement a policy.
    /// </summary>
    /// <returns>True if successfully implemented, false if cannot afford.</returns>
    public bool ImplementPolicy(GameState state, int policyId, int level = 1)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        var policy = _repository.GetPolicyById(policyId);
        if (policy == null) return false;

        // Validate level
        level = Math.Clamp(level, 1, policy.MaxLevel);

        // Check if already implemented
        if (state.ActivePolicies.Any(p => p.PolicyId == policyId))
            return false;

        // Calculate cost
        int annualCost = policy.CostPerYear * level;

        // Check affordability
        if (state.Budget.AvailableBudget < annualCost)
            return false;

        // Implement
        var activePolicy = new ActivePolicy
        {
            PolicyId = policyId,
            Level = level,
            ImplementedTurn = state.CurrentTurn,
            MonthsActive = 0
        };

        state.ActivePolicies.Add(activePolicy);
        state.Budget.AllocatedBudget += annualCost;

        _eventBus.Publish(new PolicyImplementedEvent
        {
            PolicyId = policyId,
            Level = level,
            Cost = annualCost
        });

        return true;
    }

    /// <summary>
    /// Upgrades an existing policy.
    /// </summary>
    public bool UpgradePolicy(GameState state, int policyId)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        var activePolicy = state.ActivePolicies.FirstOrDefault(p => p.PolicyId == policyId);
        if (activePolicy == null) return false;

        var policy = _repository.GetPolicyById(policyId);
        if (policy == null) return false;

        // Check if already at max level
        if (activePolicy.Level >= policy.MaxLevel)
            return false;

        // Calculate additional cost
        int currentCost = policy.CostPerYear * activePolicy.Level;
        int newCost = policy.CostPerYear * (activePolicy.Level + 1);
        int additionalCost = newCost - currentCost;

        // Check affordability
        if (state.Budget.AvailableBudget < additionalCost)
            return false;

        // Upgrade
        activePolicy.Level++;
        state.Budget.AllocatedBudget += additionalCost;

        return true;
    }

    /// <summary>
    /// Removes a policy.
    /// </summary>
    public bool RemovePolicy(GameState state, int policyId)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        var activePolicy = state.ActivePolicies.FirstOrDefault(p => p.PolicyId == policyId);
        if (activePolicy == null) return false;

        var policy = _repository.GetPolicyById(policyId);
        if (policy == null) return false;

        // Refund budget
        int annualCost = policy.CostPerYear * activePolicy.Level;
        state.Budget.AllocatedBudget -= annualCost;

        state.ActivePolicies.Remove(activePolicy);

        _eventBus.Publish(new PolicyRemovedEvent { PolicyId = policyId });

        return true;
    }

    /// <summary>
    /// Applies effects from all active policies.
    /// Called once per turn by the game loop.
    /// </summary>
    public void ApplyPolicyEffects(GameState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        foreach (var activePolicy in state.ActivePolicies)
        {
            var policy = _repository.GetPolicyById(activePolicy.PolicyId);
            if (policy == null) continue;

            foreach (var effect in policy.Effects)
            {
                if (ShouldApplyEffect(effect, activePolicy.MonthsActive))
                {
                    ApplyEffect(state, effect, activePolicy.Level);
                }
            }
        }
    }

    private bool ShouldApplyEffect(PolicyEffect effect, int monthsActive)
    {
        return effect.Type switch
        {
            EffectType.Immediate => monthsActive == 1,
            EffectType.Gradual => monthsActive > effect.DelayMonths,
            EffectType.Delayed => monthsActive == effect.DelayMonths,
            _ => false
        };
    }

    private void ApplyEffect(GameState state, PolicyEffect effect, int level)
    {
        float value = effect.EffectValue * level;
        string metric = effect.AffectsMetric;

        if (!state.Metrics.ContainsKey(metric))
        {
            state.Metrics[metric] = 0;
        }

        float oldValue = state.Metrics[metric];
        state.Metrics[metric] += value;

        _eventBus.Publish(new MetricChangedEvent
        {
            MetricName = metric,
            OldValue = oldValue,
            NewValue = state.Metrics[metric]
        });
    }

    /// <summary>
    /// Gets the total annual cost of all active policies.
    /// </summary>
    public long GetTotalPolicyCost(GameState state)
    {
        long total = 0;
        foreach (var activePolicy in state.ActivePolicies)
        {
            var policy = _repository.GetPolicyById(activePolicy.PolicyId);
            if (policy != null)
            {
                total += policy.CostPerYear * activePolicy.Level;
            }
        }
        return total;
    }
}
