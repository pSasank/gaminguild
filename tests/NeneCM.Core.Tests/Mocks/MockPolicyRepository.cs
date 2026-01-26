using NeneCM.Core.Interfaces;
using NeneCM.Core.Models;

namespace NeneCM.Core.Tests.Mocks;

/// <summary>
/// Mock policy repository for testing.
/// </summary>
public class MockPolicyRepository : IPolicyRepository
{
    private readonly List<Policy> _policies = new();

    public MockPolicyRepository()
    {
        // Add default test policies
        _policies.Add(new Policy
        {
            Id = 1,
            Name = "Test Policy A",
            Category = "agriculture",
            CostPerYear = 1000,
            MaxLevel = 5,
            Effects = new List<PolicyEffect>
            {
                new() { AffectsMetric = "approval_rural", EffectValue = 5f, Type = EffectType.Immediate },
                new() { AffectsMetric = "gdp_growth", EffectValue = 0.5f, Type = EffectType.Gradual, DelayMonths = 3 }
            }
        });

        _policies.Add(new Policy
        {
            Id = 2,
            Name = "Test Policy B",
            Category = "education",
            CostPerYear = 2000,
            MaxLevel = 3,
            Effects = new List<PolicyEffect>
            {
                new() { AffectsMetric = "approval_students", EffectValue = 10f, Type = EffectType.Immediate }
            }
        });

        _policies.Add(new Policy
        {
            Id = 3,
            Name = "Test Policy C",
            Category = "agriculture",
            CostPerYear = 500,
            MaxLevel = 5,
            Effects = new List<PolicyEffect>
            {
                new() { AffectsMetric = "farmer_income", EffectValue = 1000f, Type = EffectType.Delayed, DelayMonths = 6 }
            }
        });
    }

    public void AddPolicy(Policy policy) => _policies.Add(policy);

    public IEnumerable<Policy> GetAllPolicies() => _policies;

    public IEnumerable<Policy> GetPoliciesByCategory(string category) =>
        _policies.Where(p => p.Category == category);

    public Policy? GetPolicyById(int id) => _policies.FirstOrDefault(p => p.Id == id);

    public IEnumerable<PolicyEffect> GetPolicyEffects(int policyId) =>
        _policies.FirstOrDefault(p => p.Id == policyId)?.Effects ?? Enumerable.Empty<PolicyEffect>();
}
