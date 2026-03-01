namespace NeneCM.Core.Events;

/// <summary>
/// Base class for all game events.
/// </summary>
public abstract class GameEvent
{
    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when a turn is advanced.
/// </summary>
public class TurnAdvancedEvent : GameEvent
{
    public int NewTurn { get; init; }
    public int NewMonth { get; init; }
    public int NewYear { get; init; }
}

/// <summary>
/// Fired when a policy is implemented.
/// </summary>
public class PolicyImplementedEvent : GameEvent
{
    public int PolicyId { get; init; }
    public int Level { get; init; }
    public long Cost { get; init; }
}

/// <summary>
/// Fired when a policy is removed/cancelled.
/// </summary>
public class PolicyRemovedEvent : GameEvent
{
    public int PolicyId { get; init; }
}

/// <summary>
/// Fired when an election is triggered.
/// </summary>
public class ElectionTriggeredEvent : GameEvent
{
    public float OverallApproval { get; init; }
    public int TermNumber { get; init; }
}

/// <summary>
/// Fired when an election result is determined.
/// </summary>
public class ElectionResultEvent : GameEvent
{
    public bool Won { get; init; }
    public float Approval { get; init; }
    public int TermNumber { get; init; }
}

/// <summary>
/// Fired when the game is saved.
/// </summary>
public class GameSavedEvent : GameEvent
{
    public string SavePath { get; init; } = string.Empty;
}

/// <summary>
/// Fired when the game is loaded.
/// </summary>
public class GameLoadedEvent : GameEvent
{
    public string SavePath { get; init; } = string.Empty;
}

/// <summary>
/// Fired when a metric value changes.
/// </summary>
public class MetricChangedEvent : GameEvent
{
    public string MetricName { get; init; } = string.Empty;
    public float OldValue { get; init; }
    public float NewValue { get; init; }
}

/// <summary>
/// Fired when faction approval changes.
/// </summary>
public class FactionApprovalChangedEvent : GameEvent
{
    public int FactionId { get; init; }
    public float OldApproval { get; init; }
    public float NewApproval { get; init; }
}

/// <summary>
/// Fired when the budget changes.
/// </summary>
public class BudgetChangedEvent : GameEvent
{
    public long OldAvailable { get; init; }
    public long NewAvailable { get; init; }
}

/// <summary>
/// Fired when the game ends (player loses election in free version).
/// </summary>
public class GameOverEvent : GameEvent
{
    public string Reason { get; init; } = string.Empty;
    public int FinalTurn { get; init; }
    public float FinalApproval { get; init; }
}
