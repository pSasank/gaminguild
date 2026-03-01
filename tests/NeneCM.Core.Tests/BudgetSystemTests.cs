using NeneCM.Core.Events;
using NeneCM.Core.Models;
using NeneCM.Core.Systems;
using Xunit;

namespace NeneCM.Core.Tests;

public class BudgetSystemTests
{
    private readonly EventBus _eventBus;
    private readonly BudgetSystem _budgetSystem;

    public BudgetSystemTests()
    {
        _eventBus = new EventBus();
        _budgetSystem = new BudgetSystem(_eventBus);
    }

    private GameState CreateTestState(long totalBudget = 100000, long allocated = 0, long debt = 0)
    {
        return new GameState
        {
            Budget = new Budget
            {
                TotalBudget = totalBudget,
                AllocatedBudget = allocated,
                Debt = debt
            },
            Metrics = new Dictionary<string, float>()
        };
    }

    [Fact]
    public void ProcessYearlyBudget_UpdatesRevenueBasedOnGDP()
    {
        var state = CreateTestState();
        state.Metrics["gdp_growth"] = 10f; // 10% growth

        _budgetSystem.ProcessYearlyBudget(state, 100000);

        Assert.Equal(110000, state.Budget.TotalBudget); // 10% increase
    }

    [Fact]
    public void ProcessYearlyBudget_AppliesDebtInterest()
    {
        var state = CreateTestState(debt: 10000);

        _budgetSystem.ProcessYearlyBudget(state, 100000);

        Assert.Equal(10800, state.Budget.Debt); // 8% interest
    }

    [Fact]
    public void ProcessYearlyBudget_FiresBudgetChangedEvent()
    {
        var state = CreateTestState(50000);
        BudgetChangedEvent? receivedEvent = null;
        _eventBus.Subscribe<BudgetChangedEvent>(e => receivedEvent = e);

        _budgetSystem.ProcessYearlyBudget(state, 100000);

        Assert.NotNull(receivedEvent);
    }

    [Fact]
    public void ProcessMonthlyBudget_AddsToDebtWhenInDeficit()
    {
        var state = CreateTestState(totalBudget: 100000, allocated: 112000); // 12000 annual deficit

        _budgetSystem.ProcessMonthlyBudget(state);

        Assert.Equal(1000, state.Budget.Debt); // 12000 / 12 = 1000 monthly
    }

    [Fact]
    public void ProcessMonthlyBudget_NoDebtWhenNotInDeficit()
    {
        var state = CreateTestState(totalBudget: 100000, allocated: 80000);

        _budgetSystem.ProcessMonthlyBudget(state);

        Assert.Equal(0, state.Budget.Debt);
    }

    [Fact]
    public void IsBankrupt_TrueWhenDebtExceedsThreshold()
    {
        var state = CreateTestState(totalBudget: 100000, debt: 200000); // 200% debt ratio

        bool result = _budgetSystem.IsBankrupt(state);

        Assert.True(result);
    }

    [Fact]
    public void IsBankrupt_FalseWhenDebtBelowThreshold()
    {
        var state = CreateTestState(totalBudget: 100000, debt: 100000); // 100% debt ratio

        bool result = _budgetSystem.IsBankrupt(state);

        Assert.False(result);
    }

    [Fact]
    public void IsNearBankruptcy_TrueAt80PercentOfThreshold()
    {
        var state = CreateTestState(totalBudget: 100000, debt: 160000); // 160% = 80% of 200%

        bool result = _budgetSystem.IsNearBankruptcy(state);

        Assert.True(result);
    }

    [Fact]
    public void GetDebtRatio_CalculatesCorrectly()
    {
        var state = CreateTestState(totalBudget: 100000, debt: 50000);

        float ratio = _budgetSystem.GetDebtRatio(state);

        Assert.Equal(0.5f, ratio);
    }

    [Fact]
    public void GetBalance_ReturnsPositiveForSurplus()
    {
        var state = CreateTestState(totalBudget: 100000, allocated: 80000);

        long balance = _budgetSystem.GetBalance(state);

        Assert.Equal(20000, balance);
    }

    [Fact]
    public void GetBalance_ReturnsNegativeForDeficit()
    {
        var state = CreateTestState(totalBudget: 100000, allocated: 120000);

        long balance = _budgetSystem.GetBalance(state);

        Assert.Equal(-20000, balance);
    }

    [Fact]
    public void ShouldShowDeficitWarning_TrueWhenDeficitExceeds10Percent()
    {
        var state = CreateTestState(totalBudget: 100000, allocated: 115000); // 15% deficit

        bool result = _budgetSystem.ShouldShowDeficitWarning(state);

        Assert.True(result);
    }

    [Fact]
    public void ShouldShowDeficitWarning_FalseWhenDeficitBelow10Percent()
    {
        var state = CreateTestState(totalBudget: 100000, allocated: 105000); // 5% deficit

        bool result = _budgetSystem.ShouldShowDeficitWarning(state);

        Assert.False(result);
    }

    [Fact]
    public void TakeLoan_IncreasesDebtAndBudget()
    {
        var state = CreateTestState(totalBudget: 100000);

        bool result = _budgetSystem.TakeLoan(state, 20000);

        Assert.True(result);
        Assert.Equal(20000, state.Budget.Debt);
        Assert.Equal(120000, state.Budget.TotalBudget);
    }

    [Fact]
    public void TakeLoan_FailsIfWouldCauseBankruptcy()
    {
        var state = CreateTestState(totalBudget: 100000, debt: 150000);

        bool result = _budgetSystem.TakeLoan(state, 60000); // Would exceed 200%

        Assert.False(result);
        Assert.Equal(150000, state.Budget.Debt);
    }

    [Fact]
    public void RepayDebt_ReducesDebtAndBudget()
    {
        var state = CreateTestState(totalBudget: 100000, debt: 30000);

        bool result = _budgetSystem.RepayDebt(state, 10000);

        Assert.True(result);
        Assert.Equal(20000, state.Budget.Debt);
        Assert.Equal(90000, state.Budget.TotalBudget);
    }

    [Fact]
    public void RepayDebt_FailsIfNotEnoughAvailable()
    {
        var state = CreateTestState(totalBudget: 100000, allocated: 95000, debt: 30000);

        bool result = _budgetSystem.RepayDebt(state, 10000); // Only 5000 available

        Assert.False(result);
        Assert.Equal(30000, state.Budget.Debt);
    }

    [Fact]
    public void RepayDebt_CapsAtTotalDebt()
    {
        var state = CreateTestState(totalBudget: 100000, debt: 5000);

        _budgetSystem.RepayDebt(state, 10000); // Trying to repay more than owed

        Assert.Equal(0, state.Budget.Debt);
    }
}
