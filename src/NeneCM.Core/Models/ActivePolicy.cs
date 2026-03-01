namespace NeneCM.Core.Models;

/// <summary>
/// Represents a policy that has been implemented by the player.
/// </summary>
public class ActivePolicy
{
    /// <summary>
    /// The ID of the policy (references Policy table).
    /// </summary>
    public int PolicyId { get; set; }

    /// <summary>
    /// Current upgrade level (1-5).
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// The turn number when this policy was implemented.
    /// </summary>
    public int ImplementedTurn { get; set; }

    /// <summary>
    /// Number of months this policy has been active.
    /// </summary>
    public int MonthsActive { get; set; }

    /// <summary>
    /// Creates a deep copy of this active policy.
    /// </summary>
    public ActivePolicy Clone()
    {
        return new ActivePolicy
        {
            PolicyId = PolicyId,
            Level = Level,
            ImplementedTurn = ImplementedTurn,
            MonthsActive = MonthsActive
        };
    }
}
