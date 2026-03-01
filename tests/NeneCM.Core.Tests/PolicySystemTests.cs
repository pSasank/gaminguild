using NeneCM.Core.Events;
using NeneCM.Core.Models;
using NeneCM.Core.Systems;
using NeneCM.Core.Tests.Mocks;
using Xunit;

namespace NeneCM.Core.Tests;

public class PolicySystemTests
{
    private readonly EventBus _eventBus;
    private readonly MockPolicyRepository _repository;
    private readonly PolicySystem _policySystem;

    public PolicySystemTests()
    {
        _eventBus = new EventBus();
        _repository = new MockPolicyRepository();
        _policySystem = new PolicySystem(_repository, _eventBus);
    }

    private GameState CreateTestState(long budget = 100000)
    {
        return new GameState
        {
            Budget = new Budget { TotalBudget = budget, AllocatedBudget = 0 }
        };
    }

    [Fact]
    public void ImplementPolicy_DeductsBudget()
    {
        var state = CreateTestState(100000);

        bool result = _policySystem.ImplementPolicy(state, 1, level: 1);

        Assert.True(result);
        Assert.Equal(1000, state.Budget.AllocatedBudget); // Policy 1 costs 1000
        Assert.Equal(99000, state.Budget.AvailableBudget);
    }

    [Fact]
    public void ImplementPolicy_FailsWithInsufficientFunds()
    {
        var state = CreateTestState(500); // Not enough for policy 1 (costs 1000)

        bool result = _policySystem.ImplementPolicy(state, 1);

        Assert.False(result);
        Assert.Equal(0, state.Budget.AllocatedBudget);
        Assert.Empty(state.ActivePolicies);
    }

    [Fact]
    public void ImplementPolicy_CannotImplementSamePolicyTwice()
    {
        var state = CreateTestState(100000);

        _policySystem.ImplementPolicy(state, 1);
        bool result = _policySystem.ImplementPolicy(state, 1);

        Assert.False(result);
        Assert.Single(state.ActivePolicies);
    }

    [Fact]
    public void ImplementPolicy_HigherLevelCostsMore()
    {
        var state = CreateTestState(100000);

        _policySystem.ImplementPolicy(state, 1, level: 3);

        Assert.Equal(3000, state.Budget.AllocatedBudget); // 1000 * 3
    }

    [Fact]
    public void ImplementPolicy_ClampsLevelToMax()
    {
        var state = CreateTestState(100000);

        _policySystem.ImplementPolicy(state, 2, level: 10); // Policy 2 max is 3

        Assert.Single(state.ActivePolicies);
        Assert.Equal(3, state.ActivePolicies[0].Level);
    }

    [Fact]
    public void ImplementPolicy_FiresEvent()
    {
        var state = CreateTestState(100000);
        PolicyImplementedEvent? receivedEvent = null;
        _eventBus.Subscribe<PolicyImplementedEvent>(e => receivedEvent = e);

        _policySystem.ImplementPolicy(state, 1, level: 2);

        Assert.NotNull(receivedEvent);
        Assert.Equal(1, receivedEvent.PolicyId);
        Assert.Equal(2, receivedEvent.Level);
        Assert.Equal(2000, receivedEvent.Cost);
    }

    [Fact]
    public void RemovePolicy_RefundsBudget()
    {
        var state = CreateTestState(100000);
        _policySystem.ImplementPolicy(state, 1, level: 2);

        bool result = _policySystem.RemovePolicy(state, 1);

        Assert.True(result);
        Assert.Equal(0, state.Budget.AllocatedBudget);
        Assert.Empty(state.ActivePolicies);
    }

    [Fact]
    public void RemovePolicy_FailsForNonExistentPolicy()
    {
        var state = CreateTestState(100000);

        bool result = _policySystem.RemovePolicy(state, 999);

        Assert.False(result);
    }

    [Fact]
    public void UpgradePolicy_IncreasesLevelAndCost()
    {
        var state = CreateTestState(100000);
        _policySystem.ImplementPolicy(state, 1, level: 1);

        bool result = _policySystem.UpgradePolicy(state, 1);

        Assert.True(result);
        Assert.Equal(2, state.ActivePolicies[0].Level);
        Assert.Equal(2000, state.Budget.AllocatedBudget);
    }

    [Fact]
    public void UpgradePolicy_FailsAtMaxLevel()
    {
        var state = CreateTestState(100000);
        _policySystem.ImplementPolicy(state, 2, level: 3); // Max level for policy 2

        bool result = _policySystem.UpgradePolicy(state, 2);

        Assert.False(result);
        Assert.Equal(3, state.ActivePolicies[0].Level);
    }

    [Fact]
    public void ApplyPolicyEffects_ImmediateEffect_AppliesOnFirstMonth()
    {
        var state = CreateTestState(100000);
        state.Metrics["approval_rural"] = 50f;
        _policySystem.ImplementPolicy(state, 1, level: 1);
        state.ActivePolicies[0].MonthsActive = 1;

        _policySystem.ApplyPolicyEffects(state);

        Assert.Equal(55f, state.Metrics["approval_rural"]); // +5 from effect
    }

    [Fact]
    public void ApplyPolicyEffects_ImmediateEffect_DoesNotApplyOnSubsequentMonths()
    {
        var state = CreateTestState(100000);
        state.Metrics["approval_rural"] = 50f;
        _policySystem.ImplementPolicy(state, 1, level: 1);
        state.ActivePolicies[0].MonthsActive = 2; // Second month

        _policySystem.ApplyPolicyEffects(state);

        Assert.Equal(50f, state.Metrics["approval_rural"]); // No change
    }

    [Fact]
    public void ApplyPolicyEffects_GradualEffect_AppliesAfterDelay()
    {
        var state = CreateTestState(100000);
        state.Metrics["gdp_growth"] = 0f;
        _policySystem.ImplementPolicy(state, 1, level: 1);
        state.ActivePolicies[0].MonthsActive = 4; // After 3 month delay

        _policySystem.ApplyPolicyEffects(state);

        Assert.Equal(0.5f, state.Metrics["gdp_growth"]);
    }

    [Fact]
    public void ApplyPolicyEffects_GradualEffect_DoesNotApplyBeforeDelay()
    {
        var state = CreateTestState(100000);
        state.Metrics["gdp_growth"] = 0f;
        _policySystem.ImplementPolicy(state, 1, level: 1);
        state.ActivePolicies[0].MonthsActive = 2; // Before 3 month delay

        _policySystem.ApplyPolicyEffects(state);

        Assert.Equal(0f, state.Metrics["gdp_growth"]);
    }

    [Fact]
    public void ApplyPolicyEffects_DelayedEffect_AppliesExactlyAtDelay()
    {
        var state = CreateTestState(100000);
        state.Metrics["farmer_income"] = 0f;
        _policySystem.ImplementPolicy(state, 3, level: 1);
        state.ActivePolicies[0].MonthsActive = 6; // Exactly at delay

        _policySystem.ApplyPolicyEffects(state);

        Assert.Equal(1000f, state.Metrics["farmer_income"]);
    }

    [Fact]
    public void ApplyPolicyEffects_EffectMultipliesByLevel()
    {
        var state = CreateTestState(100000);
        state.Metrics["approval_rural"] = 50f;
        _policySystem.ImplementPolicy(state, 1, level: 3);
        state.ActivePolicies[0].MonthsActive = 1;

        _policySystem.ApplyPolicyEffects(state);

        Assert.Equal(65f, state.Metrics["approval_rural"]); // +5 * 3 = +15
    }

    [Fact]
    public void GetAvailablePolicies_ReturnsAll()
    {
        var policies = _policySystem.GetAvailablePolicies().ToList();

        Assert.Equal(3, policies.Count);
    }

    [Fact]
    public void GetAvailablePolicies_FiltersByCategory()
    {
        var policies = _policySystem.GetAvailablePolicies("agriculture").ToList();

        Assert.Equal(2, policies.Count);
        Assert.All(policies, p => Assert.Equal("agriculture", p.Category));
    }

    [Fact]
    public void GetTotalPolicyCost_CalculatesCorrectly()
    {
        var state = CreateTestState(100000);
        _policySystem.ImplementPolicy(state, 1, level: 2); // 2000
        _policySystem.ImplementPolicy(state, 2, level: 1); // 2000

        long total = _policySystem.GetTotalPolicyCost(state);

        Assert.Equal(4000, total);
    }
}
