namespace NeneCM.Core.Models;

/// <summary>
/// Represents the approval rating from a specific faction/voter group.
/// </summary>
public class FactionApproval
{
    /// <summary>
    /// The ID of the faction (references Faction table).
    /// </summary>
    public int FactionId { get; set; }

    /// <summary>
    /// Current approval rating (0-100).
    /// </summary>
    public float Approval { get; set; }

    /// <summary>
    /// Percentage of total population this faction represents.
    /// </summary>
    public float PopulationPercent { get; set; }

    /// <summary>
    /// Creates a deep copy of this faction approval.
    /// </summary>
    public FactionApproval Clone()
    {
        return new FactionApproval
        {
            FactionId = FactionId,
            Approval = Approval,
            PopulationPercent = PopulationPercent
        };
    }
}
