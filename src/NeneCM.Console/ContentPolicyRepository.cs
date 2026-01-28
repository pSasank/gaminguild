using NeneCM.Core.Data;
using NeneCM.Core.Interfaces;
using NeneCM.Core.Models;

namespace NeneCM.Console;

/// <summary>
/// Adapts StateContent to IPolicyRepository interface.
/// </summary>
public class ContentPolicyRepository : IPolicyRepository
{
    private readonly List<Policy> _policies;

    public ContentPolicyRepository(StateContent content)
    {
        _policies = content.Policies.Select(ConvertPolicy).ToList();
    }

    public IEnumerable<Policy> GetAllPolicies() => _policies;

    public IEnumerable<Policy> GetPoliciesByCategory(string category) =>
        _policies.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    public Policy? GetPolicyById(int id) => _policies.FirstOrDefault(p => p.Id == id);

    public IEnumerable<PolicyEffect> GetPolicyEffects(int policyId) =>
        _policies.FirstOrDefault(p => p.Id == policyId)?.Effects ?? Enumerable.Empty<PolicyEffect>();

    private Policy ConvertPolicy(PolicyDefinition def)
    {
        return new Policy
        {
            Id = def.Id,
            Name = def.Name,
            Category = def.Category,
            Description = def.Description,
            CostPerYear = def.Cost_per_year,
            ImplementationTime = def.Implementation_time,
            MaxLevel = def.Max_level,
            Effects = def.Effects.Select(e => new PolicyEffect
            {
                AffectsMetric = e.Metric,
                EffectValue = e.Value,
                Type = ParseEffectType(e.Type),
                DelayMonths = e.Delay
            }).ToList()
        };
    }

    private EffectType ParseEffectType(string type) => type.ToLower() switch
    {
        "immediate" => EffectType.Immediate,
        "gradual" => EffectType.Gradual,
        "delayed" => EffectType.Delayed,
        _ => EffectType.Immediate
    };
}
