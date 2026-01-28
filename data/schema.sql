-- NeneCM Database Schema
-- SQLite database for storing game content

-- Policies table
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

-- Policy effects table
CREATE TABLE IF NOT EXISTS policy_effects (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    policy_id INTEGER NOT NULL,
    affects_metric TEXT NOT NULL,
    effect_value REAL NOT NULL,
    effect_type TEXT NOT NULL DEFAULT 'immediate',
    delay_months INTEGER DEFAULT 0,
    FOREIGN KEY (policy_id) REFERENCES policies(id) ON DELETE CASCADE
);

-- Policy faction effects table
CREATE TABLE IF NOT EXISTS policy_faction_effects (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    policy_id INTEGER NOT NULL,
    faction_id INTEGER NOT NULL,
    approval_change REAL NOT NULL,
    FOREIGN KEY (policy_id) REFERENCES policies(id) ON DELETE CASCADE,
    FOREIGN KEY (faction_id) REFERENCES factions(id)
);

-- Metrics table
CREATE TABLE IF NOT EXISTS metrics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT UNIQUE NOT NULL,
    display_name TEXT NOT NULL,
    display_name_te TEXT,
    category TEXT,
    base_value REAL NOT NULL,
    min_value REAL NOT NULL,
    max_value REAL NOT NULL,
    unit TEXT
);

-- Factions table
CREATE TABLE IF NOT EXISTS factions (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    name_te TEXT,
    description TEXT,
    description_te TEXT,
    population_percent REAL NOT NULL,
    priorities TEXT  -- JSON array of category strings
);

-- Events table
CREATE TABLE IF NOT EXISTS events (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    name_te TEXT,
    category TEXT NOT NULL,
    description TEXT,
    description_te TEXT,
    probability REAL NOT NULL DEFAULT 0.1,
    trigger_conditions TEXT  -- JSON object with conditions
);

-- Event choices table
CREATE TABLE IF NOT EXISTS event_choices (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    event_id INTEGER NOT NULL,
    choice_index INTEGER NOT NULL,
    choice_text TEXT NOT NULL,
    choice_text_te TEXT,
    cost INTEGER DEFAULT 0,
    FOREIGN KEY (event_id) REFERENCES events(id) ON DELETE CASCADE
);

-- Event choice effects table
CREATE TABLE IF NOT EXISTS event_choice_effects (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    choice_id INTEGER NOT NULL,
    affects_metric TEXT,
    effect_value REAL,
    faction_id INTEGER,
    approval_change REAL,
    FOREIGN KEY (choice_id) REFERENCES event_choices(id) ON DELETE CASCADE
);

-- State configuration table
CREATE TABLE IF NOT EXISTS state_config (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    name_te TEXT,
    starting_budget INTEGER NOT NULL,
    starting_year INTEGER NOT NULL DEFAULT 2024
);

-- Save slots table
CREATE TABLE IF NOT EXISTS save_slots (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    slot_number INTEGER NOT NULL,
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

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_policies_category ON policies(category);
CREATE INDEX IF NOT EXISTS idx_policy_effects_policy ON policy_effects(policy_id);
CREATE INDEX IF NOT EXISTS idx_events_category ON events(category);
CREATE INDEX IF NOT EXISTS idx_save_slots_slot ON save_slots(slot_number);
