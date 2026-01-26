namespace NeneCM.Core.Models;

/// <summary>
/// Represents the financial state of the government.
/// All values are in crores (₹).
/// </summary>
public class Budget
{
    /// <summary>
    /// Total annual budget available (revenue).
    /// </summary>
    public long TotalBudget { get; set; }

    /// <summary>
    /// Budget currently allocated to active policies.
    /// </summary>
    public long AllocatedBudget { get; set; }

    /// <summary>
    /// Accumulated debt from deficit spending.
    /// </summary>
    public long Debt { get; set; }

    /// <summary>
    /// Available budget for new policies.
    /// </summary>
    public long AvailableBudget => TotalBudget - AllocatedBudget;

    /// <summary>
    /// Whether the government is in deficit (spending more than revenue).
    /// </summary>
    public bool IsInDeficit => AllocatedBudget > TotalBudget;

    /// <summary>
    /// Current surplus or deficit amount.
    /// Positive = surplus, Negative = deficit.
    /// </summary>
    public long Balance => TotalBudget - AllocatedBudget;

    /// <summary>
    /// Creates a deep copy of the budget.
    /// </summary>
    public Budget Clone()
    {
        return new Budget
        {
            TotalBudget = TotalBudget,
            AllocatedBudget = AllocatedBudget,
            Debt = Debt
        };
    }
}
