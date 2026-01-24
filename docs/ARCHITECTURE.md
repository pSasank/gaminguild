# Technical Architecture

Complete implementation guide for the Nene CM political strategy game.

## Technology Choices

### Why Unity?

| Factor | Unity | Godot | Native (Java/Kotlin) |
|--------|-------|-------|---------------------|
| Cross-platform | iOS + Android | iOS + Android | Android only |
| Learning curve | Medium | Medium | High |
| Community/Resources | Massive | Growing | N/A |
| UI tools | Excellent (UI Toolkit) | Good | Android XML (clunky) |
| Data-driven design | Excellent | Good | Manual |
| Asset store | Huge | Small | N/A |
| Performance | Excellent | Excellent | Best (but harder) |
| Job market | High | Low | High |
| Cost | Free (Personal) | Free | Free |

**Unity wins because:**
- Best UI tools for strategy games
- Massive community (solutions to every problem exist)
- Easy data-driven architecture (perfect for policy systems)
- Future-proof (can add iOS later)
- Google Play Games SDK integration is seamless

**Recommended Version:** Unity 2023.2 LTS

---

## Database Schema (SQLite)

### Why SQLite?

- **Zero server costs** - Runs entirely on device
- **Fast queries** - Optimized for mobile
- **Structured data** - Perfect for policies, effects
- **Easy updates** - Just replace .db file in app update
- **Small size** - 1-3 MB for all content
- **Works offline** - No internet needed

### Tables

#### Policies Table

```sql
CREATE TABLE policies (
    id INTEGER PRIMARY KEY,
    name_en TEXT NOT NULL,
    name_te TEXT,                    -- Telugu translation
    category TEXT NOT NULL,          -- 'agriculture', 'education', etc.
    description_en TEXT,
    description_te TEXT,
    cost_per_year INTEGER,           -- In crores
    implementation_time INTEGER,     -- Months to implement
    max_level INTEGER DEFAULT 5,     -- Can be upgraded 1-5
    icon_name TEXT
);
```

#### Policy Effects Table

```sql
CREATE TABLE policy_effects (
    id INTEGER PRIMARY KEY,
    policy_id INTEGER,
    affects_metric TEXT,             -- 'gdp_growth', 'employment', 'approval_rural'
    effect_value REAL,               -- +2.5, -1.0, etc.
    effect_type TEXT,                -- 'immediate', 'gradual', 'delayed'
    delay_months INTEGER,
    FOREIGN KEY (policy_id) REFERENCES policies(id)
);
```

#### Events Table

```sql
CREATE TABLE events (
    id INTEGER PRIMARY KEY,
    name_en TEXT NOT NULL,
    name_te TEXT,
    description_en TEXT,
    description_te TEXT,
    trigger_condition TEXT,          -- JSON: {"turn": ">5", "approval": "<50"}
    probability REAL,                -- 0.0 to 1.0
    category TEXT,                   -- 'natural_disaster', 'political', 'economic'
    choices TEXT                     -- JSON array of choice objects
);
```

#### Event Effects Table

```sql
CREATE TABLE event_effects (
    id INTEGER PRIMARY KEY,
    event_id INTEGER,
    choice_index INTEGER,            -- Which choice leads to this effect
    affects_metric TEXT,
    effect_value REAL,
    duration_months INTEGER,
    FOREIGN KEY (event_id) REFERENCES events(id)
);
```

#### Metrics Table

```sql
CREATE TABLE metrics (
    id INTEGER PRIMARY KEY,
    name TEXT UNIQUE,                -- 'gdp_growth', 'unemployment', etc.
    display_name_en TEXT,
    display_name_te TEXT,
    category TEXT,                   -- 'economic', 'social', 'approval'
    base_value REAL,                 -- Starting value
    min_value REAL,
    max_value REAL,
    unit TEXT                        -- '%', 'crores', 'rating'
);
```

#### Factions Table

```sql
CREATE TABLE factions (
    id INTEGER PRIMARY KEY,
    name_en TEXT NOT NULL,
    name_te TEXT,
    description_en TEXT,
    description_te TEXT,
    base_support REAL,               -- Starting approval %
    population_percent REAL,         -- % of total population
    priorities TEXT                  -- JSON: ["agriculture", "welfare"]
);
```

#### Save Slots Table

```sql
CREATE TABLE save_slots (
    id INTEGER PRIMARY KEY,
    save_name TEXT,
    state_name TEXT,                 -- 'Telangana', 'AP'
    current_turn INTEGER,
    current_year INTEGER,
    total_budget INTEGER,
    game_state_json TEXT,            -- Full serialized state
    timestamp INTEGER,
    playtime_seconds INTEGER
);
```

### Sample Data

```sql
-- Example: Rythu Bandhu Policy
INSERT INTO policies VALUES (
    1,
    'Rythu Bandhu',                          -- name_en
    'రైతు బంధు',                              -- name_te
    'agriculture',                           -- category
    'Direct cash transfer to farmers',       -- description_en
    'రైతులకు ప్రత్యక్ష నగదు బదిలీ',         -- description_te
    6000,                                    -- cost_per_year (crores)
    3,                                       -- implementation_time (months)
    5,                                       -- max_level
    'rythu_bandhu_icon'                      -- icon_name
);

-- Effects of Rythu Bandhu
INSERT INTO policy_effects VALUES
    (1, 1, 'approval_rural', 15.0, 'immediate', 0),
    (2, 1, 'gdp_growth', 0.8, 'gradual', 6),
    (3, 1, 'budget_deficit', -6000, 'immediate', 0),
    (4, 1, 'farmer_income', 25000, 'immediate', 0);
```

---

## Core Systems

### 1. GameManager.cs

Main controller that orchestrates all game systems.

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState currentState;
    public StateConfig stateConfig;

    [Header("Systems")]
    public BudgetSystem budgetSystem;
    public ApprovalSystem approvalSystem;
    public PolicySystem policySystem;
    public EventSystem eventSystem;
    public TurnManager turnManager;

    private DatabaseManager db;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AdvanceTurn()
    {
        turnManager.ProcessTurn(currentState);
        policySystem.ApplyPolicyEffects(currentState);
        budgetSystem.CalculateBudget(currentState);
        approvalSystem.UpdateApproval(currentState);
        eventSystem.CheckForEvents(currentState);

        currentState.currentMonth++;
        if (currentState.currentMonth > 12)
        {
            currentState.currentMonth = 1;
            currentState.currentYear++;
        }
        currentState.currentTurn++;

        // Check for election every 5 years
        if (currentState.currentTurn % 60 == 0)
        {
            TriggerElection();
        }

        StateManager.SaveGame(currentState);
        EventBus.Trigger("TurnAdvanced", currentState);
    }
}
```

### 2. PolicySystem.cs

Handles policy implementation and effect calculations.

```csharp
public class PolicySystem
{
    private DatabaseManager db;

    public bool ImplementPolicy(GameState state, int policyId, int level = 1)
    {
        Policy policy = db.GetPolicyById(policyId);

        int annualCost = policy.costPerYear * level;
        if (state.budget.availableBudget < annualCost)
        {
            return false;  // Cannot afford
        }

        ActivePolicy activePolicy = new ActivePolicy
        {
            policyId = policyId,
            level = level,
            implementedTurn = state.currentTurn,
            monthsActive = 0
        };

        state.activePolicies.Add(activePolicy);
        state.budget.allocatedBudget += annualCost;

        return true;
    }

    public void ApplyPolicyEffects(GameState state)
    {
        foreach (ActivePolicy activePolicy in state.activePolicies)
        {
            activePolicy.monthsActive++;
            List<PolicyEffect> effects = db.GetPolicyEffects(activePolicy.policyId);

            foreach (PolicyEffect effect in effects)
            {
                if (ShouldApplyEffect(effect, activePolicy.monthsActive))
                {
                    ApplyEffect(state, effect, activePolicy.level);
                }
            }
        }
    }
}
```

### 3. DatabaseManager.cs

SQLite interface for data operations.

```csharp
public class DatabaseManager
{
    private SQLiteConnection db;

    public DatabaseManager(string databasePath)
    {
        db = new SQLiteConnection(databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
    }

    public List<Policy> QueryPolicies(string category = null)
    {
        if (string.IsNullOrEmpty(category))
        {
            return db.Table<PolicyModel>().ToList()
                .Select(p => ModelToPolicy(p)).ToList();
        }
        else
        {
            return db.Table<PolicyModel>()
                .Where(p => p.category == category)
                .ToList()
                .Select(p => ModelToPolicy(p)).ToList();
        }
    }

    Policy ModelToPolicy(PolicyModel model)
    {
        string lang = LocalizationManager.CurrentLanguage;

        return new Policy
        {
            id = model.id,
            name = (lang == "te") ? model.name_te : model.name_en,
            category = model.category,
            description = (lang == "te") ? model.description_te : model.description_en,
            costPerYear = model.cost_per_year,
            implementationTime = model.implementation_time,
            maxLevel = model.max_level,
            iconName = model.icon_name
        };
    }
}
```

### 4. StateManager.cs

Save/load system with local and cloud support.

```csharp
public static class StateManager
{
    private static string saveFilePath =
        Application.persistentDataPath + "/gamesave.json";

    public static void SaveGame(GameState state)
    {
        string json = JsonUtility.ToJson(state, true);
        File.WriteAllText(saveFilePath, json);

        if (Social.localUser.authenticated)
        {
            SaveToCloud(json);
        }
    }

    public static GameState LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<GameState>(json);
        }

        if (Social.localUser.authenticated)
        {
            return LoadFromCloud();
        }

        return null;
    }
}
```

---

## External Integrations

### Google Play Games

```csharp
public class PlayGamesManager : MonoBehaviour
{
    void InitializePlayGames()
    {
        #if UNITY_ANDROID
        PlayGamesClientConfiguration config =
            new PlayGamesClientConfiguration.Builder()
            .EnableSavedGames()
            .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate();
        SignIn();
        #endif
    }

    public void SignIn()
    {
        Social.localUser.Authenticate((bool success) =>
        {
            if (success)
            {
                StateManager.TrySyncCloudSave();
            }
        });
    }
}
```

### Unity IAP (Premium Unlock)

```csharp
public class PurchaseManager : MonoBehaviour, IStoreListener
{
    private const string PREMIUM_UNLOCK = "com.yourstudio.nenecm.premium";

    public void BuyPremium()
    {
        if (storeController != null)
        {
            Product product = storeController.products.WithID(PREMIUM_UNLOCK);
            if (product != null && product.availableToPurchase)
            {
                storeController.InitiatePurchase(product);
            }
        }
    }

    void UnlockPremium()
    {
        PlayerPrefs.SetInt("PremiumUnlocked", 1);
        PlayerPrefs.Save();
        EventBus.Trigger("PremiumUnlocked");
    }

    public static bool IsPremium()
    {
        return PlayerPrefs.GetInt("PremiumUnlocked", 0) == 1;
    }
}
```

### Firebase Analytics

```csharp
public class AnalyticsManager : MonoBehaviour
{
    public static void LogPolicyImplemented(string policyName, int cost)
    {
        FirebaseAnalytics.LogEvent("policy_implemented",
            new Parameter("policy_name", policyName),
            new Parameter("cost", cost)
        );
    }

    public static void LogElection(int term, float approval, bool won)
    {
        FirebaseAnalytics.LogEvent("election",
            new Parameter("term", term),
            new Parameter("approval", approval),
            new Parameter("result", won ? "won" : "lost")
        );
    }
}
```

---

## Content Creation Workflow

### Adding a New State

**Step 1: Create JSON content file**

```json
{
  "policies": [
    {
      "id": 1,
      "name_en": "Rythu Bandhu",
      "name_te": "రైతు బంధు",
      "category": "agriculture",
      "description_en": "₹10,000 per acre cash assistance to farmers",
      "description_te": "రైతులకు ఎకరానికి ₹10,000 నగదు సహాయం",
      "cost_per_year": 6000,
      "implementation_time": 3,
      "max_level": 5,
      "icon_name": "rythu_bandhu_icon",
      "effects": [
        {
          "affects_metric": "approval_rural",
          "effect_value": 15.0,
          "effect_type": "immediate",
          "delay_months": 0
        }
      ]
    }
  ]
}
```

**Step 2: Import to SQLite (Editor tool)**

```csharp
[MenuItem("Tools/Import Content/Telangana Policies")]
static void ImportTelanganaPolicies()
{
    string jsonPath = "Assets/Data/Policies/telangana_policies.json";
    PolicyDataCollection data = JsonUtility.FromJson<PolicyDataCollection>(
        File.ReadAllText(jsonPath));

    DatabaseManager db = new DatabaseManager(
        "Assets/StreamingAssets/telangana.db");

    foreach (var policy in data.policies)
    {
        db.InsertPolicy(policy);
        foreach (var effect in policy.effects)
        {
            db.InsertPolicyEffect(policy.id, effect);
        }
    }
}
```

**Step 3: Create State Config (ScriptableObject)**

```csharp
[CreateAssetMenu(fileName = "StateConfig", menuName = "Game/State Config")]
public class StateConfig : ScriptableObject
{
    public string stateName;           // "Telangana"
    public string displayName_en;      // "Telangana"
    public string displayName_te;      // "తెలంగాణ"
    public string databasePath;        // "telangana.db"
    public long startingBudget;        // 250000 (crores)
    public Sprite stateMapSprite;
    public Sprite cmPortraitDefault;
    public Dictionary<string, float> initialMetrics;
}
```

---

## Build Configuration

### Player Settings

| Setting | Value |
|---------|-------|
| Platform | Android |
| Architecture | ARM64 |
| Min API Level | 24 (Android 7.0) |
| Target API Level | 34 (Android 14) |
| Scripting Backend | IL2CPP |
| API Compatibility | .NET Standard 2.1 |
| Compression | LZ4 |
| App Bundle | Yes |

### Optimization Settings

| Setting | Value |
|---------|-------|
| Strip Engine Code | Yes |
| Managed Stripping Level | High |
| Script Call Optimization | Speed |
| Texture Compression | ASTC or ETC2 |
| Audio Quality | 64kbps (UI sounds) |

### Expected APK Size

| Component | Size |
|-----------|------|
| Unity Engine Runtime | 25-30 MB |
| Scripts (IL2CPP) | 5-8 MB |
| UI Assets (compressed) | 8-12 MB |
| Database (one state) | 2-4 MB |
| Plugins (SQLite, Play Games) | 3-5 MB |
| Audio (optional) | 5-10 MB |
| **Total** | **50-70 MB** |

---

## Technical Decisions Summary

| Decision | Choice | Confidence |
|----------|--------|------------|
| Game Engine | Unity 2023.2 LTS | 0.90 |
| Database | SQLite (local) | 0.92 |
| Architecture | Local-first, offline | 0.92 |
| Monetization | Freemium + ₹79 unlock | 0.85 |
| Cloud Saves | Google Play Games | 0.88 |
| Content Format | JSON → SQLite | 0.90 |

### Key Benefits

- **Zero operational costs** - ₹142/month Play Store fee only
- **Fully offline gameplay** - No internet dependency
- **Easy content updates** - Just replace .db file
- **Scalable to multiple states** - Reuse 90% of code
- **Professional quality** - Indie budget, AAA patterns
