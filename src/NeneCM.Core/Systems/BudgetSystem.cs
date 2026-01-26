using NeneCM.Core.Events;
using NeneCM.Core.Models;

namespace NeneCM.Core.Systems;

/// <summary>
/// Manages government finances: revenue, expenses, debt.
/// </summary>
public class BudgetSystem
{
    private readonly EventBus _eventBus;

    /// <summary>
    /// Base tax rate as percentage of GDP.
    /// </summary>
    public const float BaseTaxRate = 0.15f;

    /// <summary>
    /// Warning threshold for deficit as percentage of budget.
    /// </summary>
    public const float DeficitWarningThreshold = 0.1f;

    /// <summary>
    /// Maximum debt before bankruptcy (percentage of annual budget).
    /// </summary>
    public const float BankruptcyThreshold = 2.0f;

    /// <summary>
    /// Interest rate on accumulated debt (annual).
    /// </summary>
    public const float DebtInterestRate = 0.08f;

    public BudgetSystem(EventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Calculates and updates the budget for a new fiscal year.
    /// Should be called at the start of each year (turn where month = 1).
    /// </summary>
    public void ProcessYearlyBudget(GameState state, long baseRevenue)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        long oldAvailable = state.Budget.AvailableBudget;

        // Calculate revenue based on GDP growth
        float gdpGrowth = state.Metrics.GetValueOrDefault("gdp_growth", 0f);
        float growthMultiplier = 1 + (gdpGrowth / 100f);
        long newRevenue = (long)(baseRevenue * growthMultiplier);

        state.Budget.TotalBudget = newRevenue;

        // Apply debt interest
        if (state.Budget.Debt > 0)
        {
            long interest = (long)(state.Budget.Debt * DebtInterestRate);
            state.Budget.Debt += interest;
        }

        _eventBus.Publish(new BudgetChangedEvent
        {
            OldAvailable = oldAvailable,
            NewAvailable = state.Budget.AvailableBudget
        });
    }

    /// <summary>
    /// Processes monthly budget updates.
    /// Called each turn.
    /// </summary>
    public void ProcessMonthlyBudget(GameState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        // Check for deficit - if spending more than revenue, add to debt
        if (state.Budget.IsInDeficit)
        {
            long monthlyDeficit = (state.Budget.AllocatedBudget - state.Budget.TotalBudget) / 12;
            state.Budget.Debt += monthlyDeficit;
        }
    }

    /// <summary>
    /// Checks if the government is at risk of bankruptcy.
    /// </summary>
    public bool IsNearBankruptcy(GameState state)
    {
        if (state.Budget.TotalBudget == 0) return false;
        float debtRatio = (float)state.Budget.Debt / state.Budget.TotalBudget;
        return debtRatio >= BankruptcyThreshold * 0.8f; // 80% of threshold
    }

    /// <summary>
    /// Checks if the government is bankrupt.
    /// </summary>
    public bool IsBankrupt(GameState state)
    {
        if (state.Budget.TotalBudget == 0) return state.Budget.Debt > 0;
        float debtRatio = (float)state.Budget.Debt / state.Budget.TotalBudget;
        return debtRatio >= BankruptcyThreshold;
    }

    /// <summary>
    /// Gets the deficit/surplus for the current budget.
    /// </summary>
    public long GetBalance(GameState state)
    {
        return state.Budget.Balance;
    }

    /// <summary>
    /// Gets the debt-to-revenue ratio.
    /// </summary>
    public float GetDebtRatio(GameState state)
    {
        if (state.Budget.TotalBudget == 0) return 0;
        return (float)state.Budget.Debt / state.Budget.TotalBudget;
    }

    /// <summary>
    /// Checks if a deficit warning should be shown.
    /// </summary>
    public bool ShouldShowDeficitWarning(GameState state)
    {
        if (state.Budget.TotalBudget == 0) return false;
        float deficitRatio = Math.Abs((float)state.Budget.Balance / state.Budget.TotalBudget);
        return state.Budget.IsInDeficit && deficitRatio >= DeficitWarningThreshold;
    }

    /// <summary>
    /// Attempts to take on debt to increase available budget.
    /// </summary>
    public bool TakeLoan(GameState state, long amount)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (amount <= 0) return false;

        // Check if taking this loan would cause bankruptcy
        long newDebt = state.Budget.Debt + amount;
        if (state.Budget.TotalBudget > 0)
        {
            float newRatio = (float)newDebt / state.Budget.TotalBudget;
            if (newRatio >= BankruptcyThreshold) return false;
        }

        state.Budget.Debt = newDebt;
        state.Budget.TotalBudget += amount;

        return true;
    }

    /// <summary>
    /// Attempts to repay debt.
    /// </summary>
    public bool RepayDebt(GameState state, long amount)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (amount <= 0) return false;
        if (amount > state.Budget.AvailableBudget) return false;

        long repayment = Math.Min(amount, state.Budget.Debt);
        state.Budget.Debt -= repayment;
        state.Budget.TotalBudget -= repayment;

        return true;
    }
}
