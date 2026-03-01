using System.Text.Json;

namespace NeneCM.Core.Models;

/// <summary>
/// Represents the complete state of a game at any point in time.
/// This is the primary object that gets saved/loaded.
/// </summary>
public class GameState
{
    /// <summary>
    /// Name of the state being governed (e.g., "Telangana").
    /// </summary>
    public string StateName { get; set; } = string.Empty;

    /// <summary>
    /// Current turn number (0-indexed, each turn = 1 month).
    /// </summary>
    public int CurrentTurn { get; set; }

    /// <summary>
    /// Current year (e.g., 2024).
    /// </summary>
    public int CurrentYear { get; set; } = 2024;

    /// <summary>
    /// Current month (1-12).
    /// </summary>
    public int CurrentMonth { get; set; } = 1;

    /// <summary>
    /// The government's financial state.
    /// </summary>
    public Budget Budget { get; set; } = new();

    /// <summary>
    /// All game metrics (GDP growth, unemployment, etc.).
    /// Key = metric name, Value = current value.
    /// </summary>
    public Dictionary<string, float> Metrics { get; set; } = new();

    /// <summary>
    /// List of policies currently implemented.
    /// </summary>
    public List<ActivePolicy> ActivePolicies { get; set; } = new();

    /// <summary>
    /// Approval ratings from each faction.
    /// </summary>
    public List<FactionApproval> FactionApprovals { get; set; } = new();

    /// <summary>
    /// Total playtime in seconds.
    /// </summary>
    public int PlaytimeSeconds { get; set; }

    /// <summary>
    /// Number of elections won.
    /// </summary>
    public int TermsWon { get; set; }

    /// <summary>
    /// Whether the player has premium features unlocked.
    /// </summary>
    public bool IsPremium { get; set; }

    /// <summary>
    /// Gets the current date as a formatted string.
    /// </summary>
    public string CurrentDateString => $"{GetMonthName(CurrentMonth)} {CurrentYear}";

    /// <summary>
    /// Gets the current term number (1-indexed).
    /// Each term is 60 turns (5 years).
    /// </summary>
    public int CurrentTermNumber => (CurrentTurn / 60) + 1;

    /// <summary>
    /// Gets how many turns until the next election.
    /// </summary>
    public int TurnsUntilElection => 60 - (CurrentTurn % 60);

    /// <summary>
    /// Gets whether an election should happen this turn.
    /// Elections occur at turns 60, 120, 180, etc.
    /// </summary>
    public bool IsElectionTurn => CurrentTurn > 0 && CurrentTurn % 60 == 0;

    /// <summary>
    /// Calculates the overall approval rating as a weighted average of faction approvals.
    /// </summary>
    public float OverallApproval
    {
        get
        {
            if (FactionApprovals.Count == 0) return 50f;

            float totalWeight = 0;
            float weightedSum = 0;

            foreach (var faction in FactionApprovals)
            {
                weightedSum += faction.Approval * faction.PopulationPercent;
                totalWeight += faction.PopulationPercent;
            }

            return totalWeight > 0 ? weightedSum / totalWeight : 50f;
        }
    }

    /// <summary>
    /// Creates a deep copy of the game state.
    /// </summary>
    public GameState Clone()
    {
        return new GameState
        {
            StateName = StateName,
            CurrentTurn = CurrentTurn,
            CurrentYear = CurrentYear,
            CurrentMonth = CurrentMonth,
            Budget = Budget.Clone(),
            Metrics = new Dictionary<string, float>(Metrics),
            ActivePolicies = ActivePolicies.Select(p => p.Clone()).ToList(),
            FactionApprovals = FactionApprovals.Select(f => f.Clone()).ToList(),
            PlaytimeSeconds = PlaytimeSeconds,
            TermsWon = TermsWon,
            IsPremium = IsPremium
        };
    }

    /// <summary>
    /// Serializes the game state to JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Deserializes a game state from JSON.
    /// </summary>
    public static GameState? FromJson(string json)
    {
        return JsonSerializer.Deserialize<GameState>(json);
    }

    private static string GetMonthName(int month)
    {
        string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                           "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        return month >= 1 && month <= 12 ? months[month - 1] : "???";
    }
}
