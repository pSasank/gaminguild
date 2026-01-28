using System.Text.Json;
using NeneCM.Core.Models;

namespace NeneCM.Core.Data;

/// <summary>
/// Loads state content from JSON files.
/// </summary>
public class StateContentLoader
{
    public StateContent? LoadFromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<StateContent>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public StateContent? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }
}

/// <summary>
/// Represents the complete content for a state.
/// </summary>
public class StateContent
{
    public StateInfo State { get; set; } = new();
    public List<MetricDefinition> Metrics { get; set; } = new();
    public List<FactionDefinition> Factions { get; set; } = new();
    public List<PolicyDefinition> Policies { get; set; } = new();
    public List<EventDefinition> Events { get; set; } = new();
}

public class StateInfo
{
    public string Name { get; set; } = string.Empty;
    public string Name_te { get; set; } = string.Empty;
    public long Starting_budget { get; set; }
    public int Starting_year { get; set; } = 2024;
}

public class MetricDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Display_name { get; set; } = string.Empty;
    public float Base_value { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class FactionDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Name_te { get; set; } = string.Empty;
    public float Population_percent { get; set; }
    public List<string> Priorities { get; set; } = new();
}

public class PolicyDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Name_te { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Description_te { get; set; } = string.Empty;
    public int Cost_per_year { get; set; }
    public int Implementation_time { get; set; }
    public int Max_level { get; set; } = 5;
    public List<EffectDefinition> Effects { get; set; } = new();
    public List<FactionEffectDefinition> Faction_effects { get; set; } = new();
}

public class EffectDefinition
{
    public string Metric { get; set; } = string.Empty;
    public float Value { get; set; }
    public string Type { get; set; } = "immediate";
    public int Delay { get; set; }
}

public class FactionEffectDefinition
{
    public int Faction_id { get; set; }
    public float Approval_change { get; set; }
}

public class EventDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Name_te { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Probability { get; set; }
    public Dictionary<string, object>? Trigger { get; set; }
    public List<EventChoice> Choices { get; set; } = new();
}

public class EventChoice
{
    public string Text { get; set; } = string.Empty;
    public int Cost { get; set; }
    public List<EffectDefinition> Effects { get; set; } = new();
    public List<FactionEffectDefinition> Faction_effects { get; set; } = new();
}
