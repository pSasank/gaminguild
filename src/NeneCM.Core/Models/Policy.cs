namespace NeneCM.Core.Models;

/// <summary>
/// Represents a policy that can be implemented by the player.
/// </summary>
public class Policy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Annual cost in crores.
    /// </summary>
    public int CostPerYear { get; set; }

    /// <summary>
    /// Months required to fully implement.
    /// </summary>
    public int ImplementationTime { get; set; }

    /// <summary>
    /// Maximum upgrade level (1-5).
    /// </summary>
    public int MaxLevel { get; set; } = 5;

    public string IconName { get; set; } = string.Empty;

    /// <summary>
    /// Effects this policy has on game metrics.
    /// </summary>
    public List<PolicyEffect> Effects { get; set; } = new();
}

/// <summary>
/// Represents the effect a policy has on a game metric.
/// </summary>
public class PolicyEffect
{
    /// <summary>
    /// The metric this effect modifies (e.g., "gdp_growth", "approval_rural").
    /// </summary>
    public string AffectsMetric { get; set; } = string.Empty;

    /// <summary>
    /// The value change per level.
    /// </summary>
    public float EffectValue { get; set; }

    /// <summary>
    /// When the effect applies: "immediate", "gradual", or "delayed".
    /// </summary>
    public EffectType Type { get; set; } = EffectType.Immediate;

    /// <summary>
    /// Months before effect starts (for gradual/delayed).
    /// </summary>
    public int DelayMonths { get; set; }
}

public enum EffectType
{
    /// <summary>
    /// Effect applies immediately on first turn.
    /// </summary>
    Immediate,

    /// <summary>
    /// Effect applies every turn after delay period.
    /// </summary>
    Gradual,

    /// <summary>
    /// Effect applies once at exactly the delay period.
    /// </summary>
    Delayed
}
