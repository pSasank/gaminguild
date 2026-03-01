using Microsoft.Data.Sqlite;
using NeneCM.Core.Interfaces;
using NeneCM.Core.Models;
using System.Text.Json;

namespace NeneCM.Data;

/// <summary>
/// SQLite database manager implementing policy repository.
/// </summary>
public class DatabaseManager : IPolicyRepository, IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public DatabaseManager(string connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
    }

    /// <summary>
    /// Creates database from schema file.
    /// </summary>
    public void InitializeDatabase(string schemaPath)
    {
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException("Schema file not found", schemaPath);

        var schema = File.ReadAllText(schemaPath);
        using var command = _connection.CreateCommand();
        command.CommandText = schema;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Initializes database with inline schema.
    /// </summary>
    public void InitializeDatabaseInline()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = GetSchemaScript();
        command.ExecuteNonQuery();
    }

    #region IPolicyRepository Implementation

    public IEnumerable<Policy> GetAllPolicies()
    {
        var policies = new List<Policy>();

        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM policies";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var policy = ReadPolicy(reader);
            policy.Effects = GetPolicyEffects(policy.Id).ToList();
            policies.Add(policy);
        }

        return policies;
    }

    public IEnumerable<Policy> GetPoliciesByCategory(string category)
    {
        var policies = new List<Policy>();

        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM policies WHERE category = @category";
        command.Parameters.AddWithValue("@category", category);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var policy = ReadPolicy(reader);
            policy.Effects = GetPolicyEffects(policy.Id).ToList();
            policies.Add(policy);
        }

        return policies;
    }

    public Policy? GetPolicyById(int id)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM policies WHERE id = @id";
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var policy = ReadPolicy(reader);
            policy.Effects = GetPolicyEffects(policy.Id).ToList();
            return policy;
        }

        return null;
    }

    public IEnumerable<PolicyEffect> GetPolicyEffects(int policyId)
    {
        var effects = new List<PolicyEffect>();

        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM policy_effects WHERE policy_id = @policyId";
        command.Parameters.AddWithValue("@policyId", policyId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            effects.Add(new PolicyEffect
            {
                AffectsMetric = reader.GetString(reader.GetOrdinal("affects_metric")),
                EffectValue = (float)reader.GetDouble(reader.GetOrdinal("effect_value")),
                Type = ParseEffectType(reader.GetString(reader.GetOrdinal("effect_type"))),
                DelayMonths = reader.GetInt32(reader.GetOrdinal("delay_months"))
            });
        }

        return effects;
    }

    #endregion

    #region Save/Load Game State

    public void SaveGame(GameState state, int slotNumber, string saveName = "")
    {
        var json = state.ToJson();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO save_slots
            (slot_number, save_name, state_name, current_turn, current_year, current_month,
             total_budget, game_state_json, created_at, updated_at, playtime_seconds)
            VALUES
            (@slot, @name, @state, @turn, @year, @month, @budget, @json,
             COALESCE((SELECT created_at FROM save_slots WHERE slot_number = @slot), @now),
             @now, @playtime)";

        command.Parameters.AddWithValue("@slot", slotNumber);
        command.Parameters.AddWithValue("@name", saveName);
        command.Parameters.AddWithValue("@state", state.StateName);
        command.Parameters.AddWithValue("@turn", state.CurrentTurn);
        command.Parameters.AddWithValue("@year", state.CurrentYear);
        command.Parameters.AddWithValue("@month", state.CurrentMonth);
        command.Parameters.AddWithValue("@budget", state.Budget.TotalBudget);
        command.Parameters.AddWithValue("@json", json);
        command.Parameters.AddWithValue("@now", now);
        command.Parameters.AddWithValue("@playtime", state.PlaytimeSeconds);

        command.ExecuteNonQuery();
    }

    public GameState? LoadGame(int slotNumber)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT game_state_json FROM save_slots WHERE slot_number = @slot";
        command.Parameters.AddWithValue("@slot", slotNumber);

        var json = command.ExecuteScalar() as string;
        return json != null ? GameState.FromJson(json) : null;
    }

    public List<SaveSlotInfo> GetSaveSlots()
    {
        var slots = new List<SaveSlotInfo>();

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT slot_number, save_name, state_name, current_turn, current_year,
                   playtime_seconds, updated_at
            FROM save_slots ORDER BY slot_number";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            slots.Add(new SaveSlotInfo
            {
                SlotNumber = reader.GetInt32(0),
                SaveName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                StateName = reader.GetString(2),
                CurrentTurn = reader.GetInt32(3),
                CurrentYear = reader.GetInt32(4),
                PlaytimeSeconds = reader.GetInt32(5),
                UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(6)).DateTime
            });
        }

        return slots;
    }

    public void DeleteSave(int slotNumber)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM save_slots WHERE slot_number = @slot";
        command.Parameters.AddWithValue("@slot", slotNumber);
        command.ExecuteNonQuery();
    }

    #endregion

    #region Import Content

    public void ImportPolicy(Policy policy)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO policies (id, name, name_te, category, description,
                description_te, cost_per_year, implementation_time, max_level, icon_name)
            VALUES (@id, @name, @name_te, @category, @desc, @desc_te, @cost, @time, @level, @icon)";

        command.Parameters.AddWithValue("@id", policy.Id);
        command.Parameters.AddWithValue("@name", policy.Name);
        command.Parameters.AddWithValue("@name_te", (object?)null ?? DBNull.Value);
        command.Parameters.AddWithValue("@category", policy.Category);
        command.Parameters.AddWithValue("@desc", policy.Description);
        command.Parameters.AddWithValue("@desc_te", (object?)null ?? DBNull.Value);
        command.Parameters.AddWithValue("@cost", policy.CostPerYear);
        command.Parameters.AddWithValue("@time", policy.ImplementationTime);
        command.Parameters.AddWithValue("@level", policy.MaxLevel);
        command.Parameters.AddWithValue("@icon", policy.IconName);

        command.ExecuteNonQuery();

        // Import effects
        foreach (var effect in policy.Effects)
        {
            ImportPolicyEffect(policy.Id, effect);
        }
    }

    public void ImportPolicyEffect(int policyId, PolicyEffect effect)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO policy_effects (policy_id, affects_metric, effect_value, effect_type, delay_months)
            VALUES (@policyId, @metric, @value, @type, @delay)";

        command.Parameters.AddWithValue("@policyId", policyId);
        command.Parameters.AddWithValue("@metric", effect.AffectsMetric);
        command.Parameters.AddWithValue("@value", effect.EffectValue);
        command.Parameters.AddWithValue("@type", effect.Type.ToString().ToLower());
        command.Parameters.AddWithValue("@delay", effect.DelayMonths);

        command.ExecuteNonQuery();
    }

    #endregion

    #region Helpers

    private Policy ReadPolicy(SqliteDataReader reader)
    {
        return new Policy
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Category = reader.GetString(reader.GetOrdinal("category")),
            Description = reader.IsDBNull(reader.GetOrdinal("description"))
                ? "" : reader.GetString(reader.GetOrdinal("description")),
            CostPerYear = reader.GetInt32(reader.GetOrdinal("cost_per_year")),
            ImplementationTime = reader.GetInt32(reader.GetOrdinal("implementation_time")),
            MaxLevel = reader.GetInt32(reader.GetOrdinal("max_level")),
            IconName = reader.IsDBNull(reader.GetOrdinal("icon_name"))
                ? "" : reader.GetString(reader.GetOrdinal("icon_name"))
        };
    }

    private EffectType ParseEffectType(string type)
    {
        return type.ToLower() switch
        {
            "immediate" => EffectType.Immediate,
            "gradual" => EffectType.Gradual,
            "delayed" => EffectType.Delayed,
            _ => EffectType.Immediate
        };
    }

    private string GetSchemaScript() => @"
        CREATE TABLE IF NOT EXISTS policies (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            name_te TEXT,
            category TEXT NOT NULL,
            description TEXT,
            description_te TEXT,
            cost_per_year INTEGER NOT NULL,
            implementation_time INTEGER NOT NULL DEFAULT 1,
            max_level INTEGER NOT NULL DEFAULT 5,
            icon_name TEXT
        );

        CREATE TABLE IF NOT EXISTS policy_effects (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            policy_id INTEGER NOT NULL,
            affects_metric TEXT NOT NULL,
            effect_value REAL NOT NULL,
            effect_type TEXT NOT NULL DEFAULT 'immediate',
            delay_months INTEGER DEFAULT 0,
            FOREIGN KEY (policy_id) REFERENCES policies(id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS save_slots (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            slot_number INTEGER NOT NULL UNIQUE,
            save_name TEXT,
            state_name TEXT NOT NULL,
            current_turn INTEGER NOT NULL,
            current_year INTEGER NOT NULL,
            current_month INTEGER NOT NULL,
            total_budget INTEGER NOT NULL,
            game_state_json TEXT NOT NULL,
            created_at INTEGER NOT NULL,
            updated_at INTEGER NOT NULL,
            playtime_seconds INTEGER DEFAULT 0
        );

        CREATE INDEX IF NOT EXISTS idx_policies_category ON policies(category);
        CREATE INDEX IF NOT EXISTS idx_policy_effects_policy ON policy_effects(policy_id);
    ";

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}

public class SaveSlotInfo
{
    public int SlotNumber { get; set; }
    public string SaveName { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public int CurrentTurn { get; set; }
    public int CurrentYear { get; set; }
    public int PlaytimeSeconds { get; set; }
    public DateTime UpdatedAt { get; set; }
}
