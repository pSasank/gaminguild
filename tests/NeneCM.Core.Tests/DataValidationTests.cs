using NeneCM.Core.Data;
using Xunit;

namespace NeneCM.Core.Tests;

public class DataValidationTests
{
    private readonly StateContent _content;
    private readonly string _telanganaJson;

    public DataValidationTests()
    {
        // Load the Telangana JSON for testing
        var projectRoot = GetProjectRoot();
        var jsonPath = Path.Combine(projectRoot, "data", "policies", "telangana.json");

        if (File.Exists(jsonPath))
        {
            _telanganaJson = File.ReadAllText(jsonPath);
            var loader = new StateContentLoader();
            _content = loader.LoadFromJson(_telanganaJson) ?? new StateContent();
        }
        else
        {
            _telanganaJson = "{}";
            _content = new StateContent();
        }
    }

    private static string GetProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null && !File.Exists(Path.Combine(dir, "NeneCM.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        return dir ?? Directory.GetCurrentDirectory();
    }

    [Fact]
    public void StateContent_LoadsSuccessfully()
    {
        Assert.NotNull(_content);
        Assert.NotEmpty(_content.State.Name);
    }

    [Fact]
    public void State_HasValidName()
    {
        Assert.Equal("Telangana", _content.State.Name);
        Assert.NotEmpty(_content.State.Name_te);
    }

    [Fact]
    public void State_HasPositiveStartingBudget()
    {
        Assert.True(_content.State.Starting_budget > 0, "Starting budget must be positive");
    }

    [Fact]
    public void Policies_AllHaveRequiredFields()
    {
        foreach (var policy in _content.Policies)
        {
            Assert.True(policy.Id > 0, $"Policy must have positive ID");
            Assert.NotEmpty(policy.Name);
            Assert.NotEmpty(policy.Category);
            Assert.True(policy.Cost_per_year > 0, $"Policy '{policy.Name}' must have positive cost");
            Assert.True(policy.Max_level >= 1, $"Policy '{policy.Name}' must have max_level >= 1");
        }
    }

    [Fact]
    public void Policies_HaveNoDuplicateIds()
    {
        var ids = _content.Policies.Select(p => p.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        Assert.Equal(uniqueIds.Count, ids.Count);
    }

    [Fact]
    public void Policies_AllHaveAtLeastOneEffect()
    {
        foreach (var policy in _content.Policies)
        {
            var hasEffect = policy.Effects.Count > 0 || policy.Faction_effects.Count > 0;
            Assert.True(hasEffect, $"Policy '{policy.Name}' must have at least one effect");
        }
    }

    [Fact]
    public void Policies_EffectsReferenceValidMetrics()
    {
        var metricNames = _content.Metrics.Select(m => m.Name).ToHashSet();

        foreach (var policy in _content.Policies)
        {
            foreach (var effect in policy.Effects)
            {
                Assert.True(metricNames.Contains(effect.Metric),
                    $"Policy '{policy.Name}' references unknown metric '{effect.Metric}'");
            }
        }
    }

    [Fact]
    public void Policies_FactionEffectsReferenceValidFactions()
    {
        var factionIds = _content.Factions.Select(f => f.Id).ToHashSet();

        foreach (var policy in _content.Policies)
        {
            foreach (var factionEffect in policy.Faction_effects)
            {
                Assert.True(factionIds.Contains(factionEffect.Faction_id),
                    $"Policy '{policy.Name}' references unknown faction ID {factionEffect.Faction_id}");
            }
        }
    }

    [Fact]
    public void Policies_CostsAreReasonable()
    {
        var budget = _content.State.Starting_budget;

        foreach (var policy in _content.Policies)
        {
            // No single policy should cost more than 10% of starting budget
            Assert.True(policy.Cost_per_year <= budget * 0.1,
                $"Policy '{policy.Name}' cost ({policy.Cost_per_year}) exceeds 10% of budget");
        }
    }

    [Fact]
    public void Policies_HaveValidEffectTypes()
    {
        var validTypes = new[] { "immediate", "gradual", "delayed" };

        foreach (var policy in _content.Policies)
        {
            foreach (var effect in policy.Effects)
            {
                Assert.Contains(effect.Type, validTypes);
            }
        }
    }

    [Fact]
    public void Factions_PopulationSumsTo100()
    {
        var total = _content.Factions.Sum(f => f.Population_percent);
        Assert.Equal(100f, total, 0.1f);
    }

    [Fact]
    public void Factions_AllHaveValidPriorities()
    {
        var validCategories = _content.Policies.Select(p => p.Category).Distinct().ToHashSet();
        // Add some standard categories that might not have policies yet
        validCategories.Add("agriculture");
        validCategories.Add("education");
        validCategories.Add("healthcare");
        validCategories.Add("infrastructure");
        validCategories.Add("welfare");
        validCategories.Add("industry");
        validCategories.Add("employment");

        foreach (var faction in _content.Factions)
        {
            foreach (var priority in faction.Priorities)
            {
                Assert.True(validCategories.Contains(priority),
                    $"Faction '{faction.Name}' has unknown priority category '{priority}'");
            }
        }
    }

    [Fact]
    public void Factions_HaveNoDuplicateIds()
    {
        var ids = _content.Factions.Select(f => f.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        Assert.Equal(uniqueIds.Count, ids.Count);
    }

    [Fact]
    public void Metrics_AllHaveValidRanges()
    {
        foreach (var metric in _content.Metrics)
        {
            Assert.True(metric.Min < metric.Max,
                $"Metric '{metric.Name}' min ({metric.Min}) must be less than max ({metric.Max})");
            Assert.True(metric.Base_value >= metric.Min && metric.Base_value <= metric.Max,
                $"Metric '{metric.Name}' base value ({metric.Base_value}) must be within range [{metric.Min}, {metric.Max}]");
        }
    }

    [Fact]
    public void Metrics_HaveNoDuplicateNames()
    {
        var names = _content.Metrics.Select(m => m.Name).ToList();
        var uniqueNames = names.Distinct().ToList();

        Assert.Equal(uniqueNames.Count, names.Count);
    }

    [Fact]
    public void Events_AllHaveRequiredFields()
    {
        foreach (var gameEvent in _content.Events)
        {
            Assert.True(gameEvent.Id > 0);
            Assert.NotEmpty(gameEvent.Name);
            Assert.NotEmpty(gameEvent.Category);
            Assert.True(gameEvent.Probability > 0 && gameEvent.Probability <= 1,
                $"Event '{gameEvent.Name}' probability must be between 0 and 1");
            Assert.NotEmpty(gameEvent.Choices);
        }
    }

    [Fact]
    public void Events_AllChoicesHaveText()
    {
        foreach (var gameEvent in _content.Events)
        {
            foreach (var choice in gameEvent.Choices)
            {
                Assert.NotEmpty(choice.Text);
            }
        }
    }

    [Fact]
    public void Events_HaveNoDuplicateIds()
    {
        var ids = _content.Events.Select(e => e.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        Assert.Equal(uniqueIds.Count, ids.Count);
    }

    [Fact]
    public void StartingBudget_CanAffordAtLeast5Policies()
    {
        var cheapestPolicies = _content.Policies
            .OrderBy(p => p.Cost_per_year)
            .Take(5)
            .ToList();

        var totalCost = cheapestPolicies.Sum(p => p.Cost_per_year);

        Assert.True(totalCost <= _content.State.Starting_budget,
            $"Starting budget ({_content.State.Starting_budget}) should afford at least 5 cheapest policies ({totalCost})");
    }

    [Fact]
    public void NoSinglePolicy_GivesMore30PercentApproval()
    {
        foreach (var policy in _content.Policies)
        {
            foreach (var factionEffect in policy.Faction_effects)
            {
                Assert.True(factionEffect.Approval_change <= 30,
                    $"Policy '{policy.Name}' gives {factionEffect.Approval_change}% approval to faction {factionEffect.Faction_id}, exceeds 30% limit");
            }
        }
    }

    [Fact]
    public void ContentHasMinimumPolicies()
    {
        Assert.True(_content.Policies.Count >= 10,
            $"Content should have at least 10 policies, has {_content.Policies.Count}");
    }

    [Fact]
    public void ContentHasMinimumEvents()
    {
        Assert.True(_content.Events.Count >= 3,
            $"Content should have at least 3 events, has {_content.Events.Count}");
    }

    [Fact]
    public void AllCategories_HaveAtLeastOnePolicy()
    {
        var coreCategories = new[] { "agriculture", "education", "welfare", "infrastructure" };
        var categoriesWithPolicies = _content.Policies.Select(p => p.Category).Distinct().ToHashSet();

        foreach (var category in coreCategories)
        {
            Assert.True(categoriesWithPolicies.Contains(category),
                $"Core category '{category}' should have at least one policy");
        }
    }
}
