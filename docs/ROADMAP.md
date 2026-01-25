# Development Roadmap

Implementation phases for the Nene CM political strategy game.

---

## Testing Strategy

**Principle:** Tests are written alongside features, not as an afterthought. Every phase includes automated tests to prevent regressions.

### Testing Framework

| Test Type | Framework | Purpose |
|-----------|-----------|---------|
| Unit Tests | Unity Test Framework (Edit Mode) | Test pure C# logic without Unity runtime |
| Integration Tests | Unity Test Framework (Play Mode) | Test systems working together |
| UI Tests | Unity Test Framework (Play Mode) | Test UI interactions and flows |
| Database Tests | Edit Mode + SQLite in-memory | Test queries and data integrity |

### Test Project Structure

```
Assets/
├── Scripts/
│   └── ...                      # Production code
└── Tests/
    ├── EditMode/                # Unit tests (no Unity runtime)
    │   ├── Core/
    │   │   ├── TurnManagerTests.cs
    │   │   ├── GameStateTests.cs
    │   │   └── EventBusTests.cs
    │   ├── Systems/
    │   │   ├── PolicySystemTests.cs
    │   │   ├── BudgetSystemTests.cs
    │   │   ├── ApprovalSystemTests.cs
    │   │   └── EventSystemTests.cs
    │   └── Data/
    │       ├── DatabaseManagerTests.cs
    │       └── DataValidationTests.cs
    └── PlayMode/                # Integration & UI tests
        ├── GameFlowTests.cs
        ├── SaveLoadTests.cs
        ├── UINavigationTests.cs
        └── ElectionTests.cs
```

### Continuous Integration

- Run all Edit Mode tests on every commit
- Run Play Mode tests on PR/merge to main
- Block merges if tests fail
- Track code coverage (target: 80%+ for core systems)

---

## Phase 1: Core Engine

**Focus:** Foundation and game loop

### Tasks

- [ ] Unity project setup with folder structure
- [ ] Import and configure SQLite plugin
- [ ] Design and implement database schema
- [ ] Implement `GameManager.cs` singleton
- [ ] Implement `TurnManager.cs` for turn-based logic
- [ ] Implement `StateManager.cs` for save/load
- [ ] Implement `EventBus.cs` for event communication
- [ ] Build basic UI framework (main menu, game screen)
- [ ] Create `GameState` data model
- [ ] Implement auto-save functionality

### Tests

- [ ] Set up Unity Test Framework (Edit Mode + Play Mode assemblies)
- [ ] `TurnManagerTests.cs`
  - Turn counter increments correctly
  - Month/year rollover works (month 12 → month 1, year+1)
  - Turn events are fired
- [ ] `GameStateTests.cs`
  - Default state initialization
  - State serialization/deserialization roundtrip
  - State cloning for undo functionality
- [ ] `EventBusTests.cs`
  - Subscribe/unsubscribe works
  - Events fire to all subscribers
  - No errors when no subscribers
- [ ] `SaveLoadTests.cs` (Play Mode)
  - Save creates file on disk
  - Load restores exact state
  - Corrupted save file handling
  - Multiple save slots work independently

### Deliverables

- Working turn system prototype
- Save/load functionality
- Basic navigation between screens
- **All Phase 1 tests passing**

---

## Phase 2: Game Systems

**Focus:** Core gameplay mechanics

### Tasks

- [ ] Implement `PolicySystem.cs`
  - Policy implementation logic
  - Effect application (immediate, gradual, delayed)
  - Policy upgrade system
- [ ] Implement `BudgetSystem.cs`
  - Revenue calculation
  - Expense tracking
  - Deficit/surplus management
- [ ] Implement `ApprovalSystem.cs`
  - Faction approval calculations
  - Overall approval weighted average
  - Approval decay/growth mechanics
- [ ] Implement `EventSystem.cs`
  - Event trigger conditions
  - Random event selection
  - Event choice consequences
- [ ] Implement `DatabaseManager.cs`
  - SQLite connection management
  - CRUD operations for all tables
  - Query optimization

### Tests

- [ ] `PolicySystemTests.cs`
  - Policy implementation deducts budget
  - Cannot implement policy without sufficient funds
  - Immediate effects apply on turn 1
  - Gradual effects apply after delay
  - Delayed effects apply exactly at delay
  - Policy upgrade multiplies effects correctly
  - Removing policy stops effects
- [ ] `BudgetSystemTests.cs`
  - Revenue calculated correctly from tax rate + GDP
  - Expenses sum all active policy costs
  - Deficit triggers warning at threshold
  - Cannot go below minimum budget (bankruptcy)
  - Year-end budget reset works
- [ ] `ApprovalSystemTests.cs`
  - Faction approval changes from policy effects
  - Approval clamped to 0-100 range
  - Weighted average calculation correct
  - Natural approval decay over time
  - Approval boost from positive events
- [ ] `EventSystemTests.cs`
  - Events trigger when conditions met
  - Probability-based selection works
  - Event choices apply correct effects
  - Events don't re-trigger inappropriately
  - Edge case: no valid events available
- [ ] `DatabaseManagerTests.cs` (using in-memory SQLite)
  - CRUD operations for all tables
  - Query by category filters correctly
  - Foreign key relationships maintained
  - Localized text retrieval (EN/TE)
  - Database versioning/migration
- [ ] `SystemIntegrationTests.cs` (Play Mode)
  - Policy → Budget → Approval chain works
  - Turn advancement updates all systems
  - Event effects propagate correctly
  - 10-turn simulation produces expected state

### Deliverables

- All core systems functional
- Systems communicating via EventBus
- **All Phase 2 tests passing (unit + integration)**

---

## Phase 3: First State Content (Telangana)

**Focus:** Real game content

### Tasks

- [ ] Research Telangana government policies
  - Rythu Bandhu
  - Kalyana Lakshmi
  - KCR Kit
  - Mission Bhagiratha
  - T-Hub initiatives
- [ ] Create `telangana_policies.json` with 30+ policies
- [ ] Create `telangana_events.json` with 20+ events
- [ ] Define faction groups
  - Rural farmers
  - Urban professionals
  - Students
  - Women
  - Business owners
- [ ] Set up initial metrics and starting values
- [ ] Create `StateConfig` ScriptableObject for Telangana
- [ ] Balance policy costs vs. effects

### Tests

- [ ] `DataValidationTests.cs`
  - All policies have required fields (name, cost, category)
  - All policies have at least one effect
  - No duplicate policy IDs
  - Cost values are positive integers
  - Effect values are within reasonable bounds
  - All referenced metrics exist
- [ ] `ContentIntegrityTests.cs`
  - All events have valid trigger conditions
  - All event choices have effects defined
  - Faction priorities reference valid categories
  - Localized strings exist for all content (EN + TE)
  - Icon names reference existing assets
- [ ] `BalanceTests.cs`
  - Starting budget can afford at least 5 policies
  - No single policy gives >30% approval boost
  - Bankruptcy not possible in first 12 turns with reasonable play
  - Election winnable with balanced strategy
  - Full 60-turn simulation completes without errors
- [ ] `GameplaySimulationTests.cs` (Play Mode)
  - Random policy selection simulation (100 runs)
  - Verify no NaN or infinity values in metrics
  - Verify no exceptions during extended play
  - Memory stable over 60-turn game

### Deliverables

- Complete Telangana database
- Playable from start to first election
- Initial balance pass
- **All content validation tests passing**

---

## Phase 4: User Interface

**Focus:** Polish and usability

### Tasks

- [ ] Design main dashboard UI
  - Approval rating display
  - Budget overview
  - Key metrics (GDP, unemployment, etc.)
  - Turn/date display
- [ ] Design policy browser screen
  - Category filters
  - Policy cards with costs/effects
  - Implementation confirmation
- [ ] Design event popup system
  - Event description
  - Choice buttons
  - Consequence preview
- [ ] Design faction details screen
  - Faction list with approval bars
  - Faction priorities display
- [ ] Design election results screen
  - Vote breakdown
  - Win/lose animations
- [ ] Implement UI animations and transitions
- [ ] Add sound effects for key actions
- [ ] Create/source UI sprite assets

### Tests

- [ ] `UINavigationTests.cs` (Play Mode)
  - Main menu → Game screen navigation
  - All buttons are clickable
  - Back/close buttons work on all popups
  - No dead-end screens
- [ ] `DashboardUITests.cs` (Play Mode)
  - Approval rating updates after turn
  - Budget display matches game state
  - Metrics display correct values
  - Turn/date display updates correctly
- [ ] `PolicyBrowserUITests.cs` (Play Mode)
  - All policies displayed
  - Category filter shows correct policies
  - Policy card shows correct cost/effects
  - Implement button disabled when unaffordable
  - Confirmation dialog appears before implementation
- [ ] `EventPopupUITests.cs` (Play Mode)
  - Event text displays correctly
  - All choices are clickable
  - Popup closes after choice
  - Effects applied after choice
- [ ] `UIRegressionTests.cs` (Play Mode)
  - No UI elements overlap
  - Text fits within bounds
  - UI scales correctly at different resolutions (16:9, 18:9, 20:9)
  - Touch targets meet minimum size (48x48 dp)

### Deliverables

- Complete UI for all screens
- Smooth animations
- Audio feedback
- **All UI tests passing**

---

## Phase 5: Localization

**Focus:** Telugu language support

### Tasks

- [ ] Set up localization system
- [ ] Translate all policy names and descriptions
- [ ] Translate all event text
- [ ] Translate UI strings
- [ ] Test Telugu font rendering on devices
- [ ] Create language toggle in settings

### Tests

- [ ] `LocalizationTests.cs`
  - All English strings have Telugu translations
  - No missing localization keys
  - Language switch updates all visible text
  - Fallback to English if translation missing
- [ ] `TeluguRenderingTests.cs` (Play Mode)
  - Telugu text renders without boxes/missing glyphs
  - Text fits within UI bounds (Telugu often longer)
  - Font loads correctly on device
  - Mixed English/Telugu text renders properly
- [ ] `LocalizationRegressionTests.cs`
  - Adding new content doesn't break existing translations
  - Database queries return correct language text
  - Settings persist language preference

### Deliverables

- Full Telugu translation
- Language switching works correctly
- No text overflow issues
- **All localization tests passing**

---

## Phase 6: External Integrations

**Focus:** Platform services

### Tasks

- [ ] Google Play Games integration
  - SDK setup
  - Sign-in flow
  - Cloud save implementation
  - Conflict resolution
- [ ] Unity IAP integration
  - Premium unlock product
  - Purchase flow
  - Receipt validation
  - Restore purchases
- [ ] Firebase Analytics integration
  - SDK setup
  - Key event tracking
  - Funnel analysis setup

### Tests

- [ ] `CloudSaveTests.cs` (Play Mode, mocked)
  - Save uploads to cloud when signed in
  - Load retrieves from cloud
  - Conflict resolution picks correct save (longest playtime)
  - Offline mode falls back to local save
  - Sign-out clears cloud session
- [ ] `IAPTests.cs` (Play Mode, mocked)
  - Purchase flow completes
  - Premium unlock persists after restart
  - Restore purchases works
  - Receipt validation rejects invalid receipts
  - Graceful handling of purchase cancellation
- [ ] `AnalyticsTests.cs` (mocked)
  - Key events logged (policy_implemented, election, etc.)
  - No PII in analytics data
  - Events batched correctly
- [ ] `IntegrationMockTests.cs`
  - Game works fully offline
  - Failed cloud save doesn't block gameplay
  - Failed analytics doesn't crash game

### Deliverables

- Cloud saves working
- Premium purchase working
- Analytics dashboard configured
- **All integration tests passing**

---

## Phase 7: Polish & Optimization

**Focus:** Production readiness

### Tasks

- [ ] Performance profiling on target devices
- [ ] Memory optimization
- [ ] Load time optimization
- [ ] Battery usage optimization
- [ ] Fix visual bugs
- [ ] Implement loading screens
- [ ] Add tutorial/onboarding flow
- [ ] Create app icon and store graphics
- [ ] Write store description

### Tests

- [ ] `PerformanceTests.cs` (Play Mode)
  - Frame rate stays above 30 FPS during gameplay
  - Turn processing completes in < 500ms
  - UI transitions complete in < 300ms
  - No frame drops during animations
- [ ] `MemoryTests.cs` (Play Mode)
  - Memory usage stays under 512MB
  - No memory leaks over 100-turn game
  - Scene transitions don't leak memory
  - Database connections properly closed
- [ ] `LoadTimeTests.cs` (Play Mode)
  - Initial load < 5 seconds
  - Scene transitions < 2 seconds
  - Save/load operations < 1 second
- [ ] `TutorialTests.cs` (Play Mode)
  - Tutorial completes without errors
  - Skip tutorial option works
  - Tutorial doesn't replay after completion
  - Tutorial state persists across sessions

### Deliverables

- 60 FPS on mid-range devices
- < 3 second load time
- Store-ready assets
- **All performance tests passing**

---

## Phase 8: QA & Beta Testing

**Focus:** Quality assurance and user feedback

### Tasks

- [ ] Internal playtesting
  - Complete multiple playthroughs
  - Document bugs and issues
- [ ] Balance testing
  - Are any policies too powerful?
  - Is bankruptcy too easy/hard?
  - Is winning re-election achievable?
- [ ] Beta testing (100 users)
  - Create Google Play internal testing track
  - Recruit testers
  - Collect feedback
- [ ] Bug fixes from beta
- [ ] Final balance adjustments
- [ ] Device compatibility testing
  - Low-end devices
  - High-end devices
  - Various screen sizes

### Tests

- [ ] `FullGameRegressionTests.cs` (Play Mode)
  - Complete game from start to election (automated)
  - Win scenario simulation
  - Lose scenario simulation
  - All edge cases covered (bankruptcy, max approval, etc.)
- [ ] `DeviceCompatibilityTests.cs`
  - Test on min-spec device (API 24, 2GB RAM)
  - Test on various screen ratios
  - Test with different system languages
- [ ] `StressTests.cs`
  - Rapid turn advancement (100 turns in 10 seconds)
  - Rapid save/load cycles
  - Memory pressure scenarios
  - Low storage scenarios
- [ ] Run full test suite on release build
  - All Edit Mode tests pass
  - All Play Mode tests pass
  - No test flakiness

### Deliverables

- Stable, bug-free build
- Balanced gameplay
- Positive beta feedback
- **100% test pass rate on release candidate**

---

## Phase 9: Launch

**Focus:** Go to market

### Tasks

- [ ] Soft launch (one region, e.g., Telangana only)
- [ ] Monitor crash reports
- [ ] Monitor analytics
- [ ] Address critical issues
- [ ] Full launch (India-wide)
- [ ] Marketing
  - Social media presence
  - Press outreach
  - Community building

### Tests

- [ ] `SmokeTests.cs` - Quick sanity check for hotfixes
  - App launches
  - New game starts
  - Turn advances
  - Save/load works
  - IAP flow accessible
- [ ] Post-launch monitoring
  - Crash-free rate > 99.5%
  - ANR rate < 0.5%
  - No critical bugs in crash reports

### Deliverables

- Live on Google Play Store
- Positive reviews
- Healthy retention metrics
- **Smoke tests gate all hotfix releases**

---

## Phase 10: Additional States

**Focus:** Expand to more Indian states

### Andhra Pradesh

- [ ] Research AP-specific policies
  - YSR Rythu Bharosa
  - Amma Vodi
  - Jagananna Vidya Deevena
  - Nadu-Nedu
  - Village/Ward Secretariats
- [ ] Create `andhra_pradesh_policies.json` (30+ policies)
- [ ] Create `andhra_pradesh_events.json` (20+ events)
- [ ] Define AP faction groups
  - Coastal Andhra farmers
  - Rayalaseema communities
  - Urban IT professionals
  - Backward classes
  - Minorities
- [ ] Create `StateConfig` for AP
- [ ] Add Telugu translations (same as Telangana)
- [ ] Balance and playtest

### Karnataka

- [ ] Research Karnataka policies
  - Gruha Lakshmi
  - Shakti (free bus travel)
  - Anna Bhagya
  - Yuva Nidhi
  - Gruha Jyoti
- [ ] Create `karnataka_policies.json`
- [ ] Create `karnataka_events.json`
- [ ] Define Karnataka factions
  - Bangalore tech workers
  - North Karnataka farmers
  - Coastal communities
  - Old Mysore region
- [ ] Add Kannada language support
- [ ] Balance and playtest

### Tamil Nadu

- [ ] Research TN policies
  - Kalaignar Breakfast Scheme
  - Free bus travel for women
  - Makkalai Thedi Maruthuvam
  - Naan Mudhalvan
- [ ] Create content files
- [ ] Add Tamil language support
- [ ] Balance and playtest

### Maharashtra

- [ ] Research Maharashtra policies
  - Ladki Bahin Yojana
  - Mukhyamantri Majhi Ladki Bahin
  - Farm loan waivers
- [ ] Create content files
- [ ] Add Marathi language support
- [ ] Balance and playtest

### Tests for Each State

- [ ] `StateContentValidationTests.cs`
  - All policies have required fields
  - All effects reference valid metrics
  - No duplicate IDs across states
- [ ] `StateBalanceTests.cs`
  - Starting budget appropriate for state GDP
  - Policies balanced for state context
  - 60-turn simulation passes
- [ ] `CrossStateRegressionTests.cs`
  - Loading one state doesn't affect another
  - Save files are state-specific
  - Language switching works per state

### Deliverables

- 4 additional playable states
- 4 new language packs
- **All state tests passing**

---

## Phase 11: iOS Release

**Focus:** Expand to Apple ecosystem

### Tasks

- [ ] iOS build configuration
  - Set up Xcode project
  - Configure signing certificates
  - Set up provisioning profiles
- [ ] Replace Google Play Games with Game Center
  - Achievements mapping
  - Leaderboards setup
  - Cloud save via iCloud
- [ ] Apple Sign-In integration
  - Required for apps with third-party login
  - Fallback for anonymous play
- [ ] iOS-specific UI adjustments
  - Safe area handling (notch, Dynamic Island)
  - Haptic feedback (Taptic Engine)
  - iOS system fonts
- [ ] StoreKit integration
  - Replace Unity IAP Android with iOS
  - Same ₹79 (converted) price point
  - Subscription option consideration
- [ ] App Store assets
  - Screenshots for all device sizes
  - App Preview video
  - App Store description
- [ ] TestFlight beta
  - Internal testing
  - External beta (100 users)
- [ ] App Store submission
  - Privacy policy
  - Age rating questionnaire
  - Review notes

### Tests

- [ ] `iOSIntegrationTests.cs`
  - Game Center sign-in works
  - iCloud save/load works
  - StoreKit purchase flow works
- [ ] `iOSUITests.cs`
  - Safe area insets respected
  - All resolutions supported (iPhone SE to Pro Max)
  - iPad layouts work
- [ ] `CrossPlatformTests.cs`
  - Cloud save syncs between Android and iOS
  - Premium purchase recognized on both platforms
  - Same gameplay experience

### Deliverables

- Live on App Store
- Feature parity with Android
- **All iOS tests passing**

---

## Phase 12: Achievements & Leaderboards

**Focus:** Player progression and competition

### Achievements

- [ ] Design achievement list (30+ achievements)
  - **Beginner**: "First Term" - Complete first election
  - **Policy Master**: "Full House" - Implement 20 policies simultaneously
  - **Budget Hawk**: "Surplus CM" - End year with budget surplus
  - **People's Champion**: "90% Club" - Reach 90% approval
  - **Survivor**: "Against All Odds" - Win election with <55% approval
  - **Speedrunner**: "Efficiency Expert" - Win Term 1 in under 30 minutes
  - **Explorer**: "Policy Wonk" - Try every policy category
  - **Crisis Manager**: "Disaster Recovery" - Recover from 3 disasters
  - **State Hopper**: "National Leader" - Win election in 3 states
  - **Hidden**: "Easter Egg" - Find secret event
- [ ] Implement `AchievementSystem.cs`
  - Track progress toward achievements
  - Trigger unlock notifications
  - Sync with platform (Play Games / Game Center)
- [ ] Design achievement UI
  - Achievement list screen
  - Progress indicators
  - Unlock animations
- [ ] Integrate with Google Play Games / Game Center

### Leaderboards

- [ ] Design leaderboard categories
  - **Highest Approval** - Peak approval rating achieved
  - **Longest Streak** - Consecutive terms won
  - **Budget Master** - Highest budget surplus
  - **Speed Run** - Fastest Term 1 completion
  - **Per-State Rankings** - Separate leaderboards per state
- [ ] Implement `LeaderboardSystem.cs`
  - Submit scores at key moments
  - Fetch and display rankings
  - Handle offline gracefully
- [ ] Design leaderboard UI
  - Top 100 display
  - Player's rank highlight
  - Friend rankings (if available)

### Tests

- [ ] `AchievementTests.cs`
  - Each achievement triggers correctly
  - No false positives
  - Progress tracking accurate
  - Platform sync works
- [ ] `LeaderboardTests.cs`
  - Score submission works
  - Rankings display correctly
  - Offline handling graceful

### Deliverables

- 30+ achievements
- 5+ leaderboard categories
- **All achievement/leaderboard tests passing**

---

## Phase 13: Challenge Modes

**Focus:** Replayability and variety

### Challenge Types

- [ ] **Scenario Mode**: Start with specific conditions
  - "Drought Crisis" - Start mid-drought, fix agriculture
  - "Budget Deficit" - Start with massive debt
  - "Unpopular Predecessor" - Start with 30% approval
  - "Election Year" - Only 12 turns to win
- [ ] **Daily Challenge**: Same seed for all players
  - Random starting conditions
  - Compare scores on leaderboard
  - Resets every 24 hours
- [ ] **Ironman Mode**: No save scumming
  - Single save slot, auto-overwrite
  - Permadeath on election loss
  - Special achievements
- [ ] **Sandbox Mode**: Unlimited resources
  - No budget constraints
  - For experimentation
  - No achievements/leaderboards

### Implementation

- [ ] `ChallengeManager.cs`
  - Load challenge configurations
  - Apply starting conditions
  - Track challenge-specific rules
- [ ] `DailyChallengeSystem.cs`
  - Fetch daily seed from server (or deterministic from date)
  - Submit scores
  - Show global rankings
- [ ] Challenge selection UI
  - Challenge list with descriptions
  - Difficulty ratings
  - Best scores per challenge

### Tests

- [ ] `ScenarioTests.cs`
  - Each scenario loads correctly
  - Starting conditions applied
  - Victory conditions work
- [ ] `DailyChallengeTests.cs`
  - Same seed produces same game
  - Score submission works
  - Leaderboard updates
- [ ] `IronmanTests.cs`
  - Save restrictions enforced
  - Permadeath works
  - Achievements unlock

### Deliverables

- 10+ scenarios
- Daily challenge system
- Ironman and Sandbox modes
- **All challenge tests passing**

---

## Phase 14: Multiplayer

**Focus:** Competitive and cooperative play

### Competitive Mode: "Election Battle"

- [ ] 2-4 players compete as CMs of same state
  - Turn-based simultaneous play
  - Shared events affect all players
  - Compare approval at election
  - Winner is "National Party Leader"
- [ ] Matchmaking system
  - Quick match
  - Private rooms (room codes)
  - Ranked matches (optional)
- [ ] Real-time sync
  - State synchronization
  - Conflict resolution
  - Disconnect handling

### Cooperative Mode: "Coalition Government"

- [ ] 2 players share governance
  - Divide policy categories
  - Shared budget
  - Combined approval rating
- [ ] Communication
  - In-game chat
  - Policy proposals
  - Vote on decisions

### Technical Requirements

- [ ] Backend service (Firebase Realtime DB or PlayFab)
  - Match creation
  - State synchronization
  - Leaderboards
- [ ] `MultiplayerManager.cs`
  - Connection handling
  - State sync
  - Latency compensation
- [ ] `MatchmakingSystem.cs`
  - Queue management
  - Skill-based matching
  - Room management

### Tests

- [ ] `MultiplayerSyncTests.cs`
  - State remains consistent
  - Events sync correctly
  - Turn order maintained
- [ ] `MatchmakingTests.cs`
  - Players matched correctly
  - Room codes work
  - Disconnect handling
- [ ] `MultiplayerRegressionTests.cs`
  - Single-player unaffected
  - Offline mode still works

### Deliverables

- Competitive multiplayer (2-4 players)
- Cooperative multiplayer (2 players)
- **All multiplayer tests passing**

---

## Content Pipeline

### Workflow for Adding New Content

```
1. Research
   ├── Government policy documents
   ├── News articles
   ├── Budget documents
   └── Expert interviews

2. Design (JSON)
   ├── policies.json
   ├── events.json
   ├── factions.json
   └── metrics.json

3. Review
   ├── Accuracy check
   ├── Balance review
   └── Localization review

4. Import
   ├── Run ContentImporter tool
   ├── Generate SQLite database
   └── Validate with tests

5. Test
   ├── DataValidationTests
   ├── BalanceTests
   └── PlaythroughTests

6. Ship
   ├── Include in build
   └── Or deliver as DLC
```

### Content JSON Schema

```json
{
  "policy": {
    "id": "integer (unique)",
    "name_en": "string (required)",
    "name_te": "string (optional)",
    "category": "enum: agriculture|education|health|infrastructure|welfare|industry|governance",
    "description_en": "string (required)",
    "description_te": "string (optional)",
    "cost_per_year": "integer (crores)",
    "implementation_time": "integer (months)",
    "max_level": "integer (1-5)",
    "icon_name": "string (asset reference)",
    "prerequisites": ["policy_id array (optional)"],
    "mutually_exclusive": ["policy_id array (optional)"],
    "effects": [
      {
        "affects_metric": "string (metric name)",
        "effect_value": "float",
        "effect_type": "enum: immediate|gradual|delayed",
        "delay_months": "integer (if delayed/gradual)"
      }
    ]
  }
}
```

### Content Validation Rules

| Rule | Enforcement |
|------|-------------|
| Unique IDs | DataValidationTests |
| Required fields present | DataValidationTests |
| Cost > 0 | DataValidationTests |
| Effects reference valid metrics | DataValidationTests |
| Translations exist for all strings | LocalizationTests |
| Icons exist in asset bundle | ContentIntegrityTests |
| No circular prerequisites | ContentIntegrityTests |
| Balance within bounds | BalanceTests |

---

## Release Checklist

### Pre-Release (1 week before)

- [ ] All tests passing (Edit Mode + Play Mode)
- [ ] Code coverage meets targets
- [ ] No critical/high bugs in backlog
- [ ] Performance benchmarks met
- [ ] Memory benchmarks met
- [ ] All content reviewed and approved
- [ ] Localization complete and reviewed
- [ ] Legal review complete (if needed)
- [ ] Privacy policy updated
- [ ] Age rating accurate

### Build Preparation

- [ ] Version number incremented
- [ ] Release notes written
- [ ] Build created with release configuration
- [ ] Build signed with release keys
- [ ] APK/AAB size within limits
- [ ] Smoke tests pass on release build

### Store Submission

- [ ] Screenshots updated (if needed)
- [ ] Store description updated (if needed)
- [ ] What's new section written
- [ ] Content rating questionnaire accurate
- [ ] Target API level compliant
- [ ] Submit to internal testing track

### Testing

- [ ] Internal QA sign-off
- [ ] Smoke tests on multiple devices
- [ ] Regression test on release build
- [ ] Verify IAP works in production
- [ ] Verify cloud save works in production
- [ ] Verify analytics events firing

### Go Live

- [ ] Promote to production track
- [ ] Monitor crash reports (first 24 hours)
- [ ] Monitor reviews (first 24 hours)
- [ ] Respond to critical issues
- [ ] Social media announcement
- [ ] Update website/landing page

### Post-Release (1 week after)

- [ ] Analyze crash-free rate
- [ ] Analyze retention metrics
- [ ] Analyze conversion metrics
- [ ] Collect user feedback
- [ ] Prioritize fixes for next release
- [ ] Retrospective meeting

---

## Post-Launch Metrics & KPIs

### Stability Metrics

| Metric | Target | Red Flag |
|--------|--------|----------|
| Crash-free users | > 99.5% | < 99% |
| ANR rate | < 0.5% | > 1% |
| Startup crash rate | < 0.1% | > 0.5% |

### Engagement Metrics

| Metric | Target | Red Flag |
|--------|--------|----------|
| DAU (Daily Active Users) | Growth | -20% week-over-week |
| Session length | > 10 min | < 5 min |
| Sessions per DAU | > 2 | < 1.5 |
| D1 retention | > 40% | < 25% |
| D7 retention | > 20% | < 10% |
| D30 retention | > 10% | < 5% |

### Monetization Metrics

| Metric | Target | Red Flag |
|--------|--------|----------|
| Free-to-premium conversion | > 5% | < 2% |
| ARPU (Avg Revenue Per User) | > ₹5 | < ₹2 |
| ARPPU (Per Paying User) | ₹79 | N/A (single price) |
| Time to first purchase | < 7 days | > 14 days |

### Gameplay Metrics

| Metric | Healthy Range |
|--------|--------------|
| Avg turns per session | 5-15 |
| Election win rate | 40-60% |
| Most popular policy category | Even distribution |
| Avg policies implemented | 8-15 per term |
| Bankruptcy rate | < 10% |
| Tutorial completion | > 80% |

### Funnel Analysis

```
Install
  └─> Tutorial Start (target: 95%)
        └─> Tutorial Complete (target: 80%)
              └─> First Turn (target: 90%)
                    └─> Turn 10 (target: 60%)
                          └─> First Election (target: 40%)
                                └─> Premium Upgrade Shown (target: 100%)
                                      └─> Premium Purchased (target: 5-10%)
                                            └─> Term 2 Started (target: 80%)
```

---

## Marketing & Community Strategy

### Pre-Launch

- [ ] Create social media accounts
  - Twitter/X
  - Instagram
  - YouTube
  - Discord server
- [ ] Build landing page
  - Game description
  - Screenshots
  - Email signup for launch notification
- [ ] Create press kit
  - High-res screenshots
  - Logo assets
  - Game description
  - Developer bio
  - Contact info
- [ ] Reach out to gaming press
  - Indian gaming sites
  - Political game reviewers
  - Mobile game YouTubers
- [ ] Build community
  - Share development updates
  - Behind-the-scenes content
  - Ask for feedback on policies

### Launch Week

- [ ] Coordinate launch announcement
  - Social media posts
  - Press release
  - Email to subscribers
- [ ] Engage with early reviews
  - Respond to feedback
  - Fix critical issues quickly
- [ ] Run launch promotion
  - Featured placement (if possible)
  - Cross-promotion with similar games

### Ongoing

- [ ] Weekly social media content
  - Tips and tricks
  - Player highlights
  - Upcoming features
- [ ] Monthly update communication
  - What's new
  - What's coming
  - Community highlights
- [ ] Community events
  - Weekly challenges
  - State-specific events
  - Real election tie-ins
- [ ] User-generated content
  - Share player achievements
  - Highlight strategies
  - Feature fan art (if any)

---

## Maintenance & Support

### Bug Triage Process

| Severity | Response Time | Resolution Time |
|----------|---------------|-----------------|
| Critical (crash, data loss) | < 4 hours | < 24 hours |
| High (major feature broken) | < 24 hours | < 3 days |
| Medium (minor feature broken) | < 3 days | < 1 week |
| Low (cosmetic, minor) | < 1 week | Next release |

### Hotfix Process

1. Identify issue from crash reports/reviews
2. Reproduce locally
3. Write failing test
4. Fix issue
5. Verify test passes
6. Run full smoke test suite
7. Build hotfix release
8. Submit to store (expedited review if critical)
9. Monitor after release

### Regular Maintenance

- [ ] Weekly: Review crash reports
- [ ] Weekly: Review user feedback/reviews
- [ ] Weekly: Check analytics dashboards
- [ ] Monthly: Dependency updates (Unity, plugins)
- [ ] Monthly: Performance regression check
- [ ] Quarterly: Security audit
- [ ] Quarterly: Content freshness review (new policies?)

### End-of-Life Planning

| Milestone | Action |
|-----------|--------|
| 1 year | Evaluate player base, plan major update or sunset |
| 18 months | If sunsetting, announce 6-month notice |
| 2 years | Remove from store, keep servers running |
| 2.5 years | Disable cloud features, game fully offline |

---

## Budget & Resources

### Development Costs (Estimated)

| Item | Cost |
|------|------|
| Unity Personal | Free |
| SQLite plugin | Free |
| Google Play Console | $25 (one-time) |
| Apple Developer | $99/year |
| Firebase | Free tier |
| Art assets | $500-2000 (or DIY) |
| Sound effects | $100-500 (or free) |
| Telugu translation | $200-500 (or community) |
| Testing devices | $300-500 (2-3 Android phones) |

### Ongoing Costs

| Item | Monthly Cost |
|------|--------------|
| Google Play | $0 |
| Firebase (free tier) | $0 |
| Domain/hosting (landing page) | $10-20 |
| Total | $10-20/month |

### Revenue Projections (Conservative)

| Scenario | Downloads | Conversion | Revenue |
|----------|-----------|------------|---------|
| Low | 10,000 | 3% | ₹23,700 |
| Medium | 50,000 | 5% | ₹197,500 |
| High | 200,000 | 7% | ₹1,106,000 |

*Based on ₹79 premium price, 30% store cut*

---

## Version History Template

### v1.0.0 (Launch)

- Initial release
- Telangana state
- English + Telugu languages
- Core gameplay loop
- Premium unlock

### v1.1.0 (Post-Launch Polish)

- Bug fixes from launch feedback
- Balance adjustments
- Performance improvements
- Additional events

### v1.2.0 (Andhra Pradesh)

- New state: Andhra Pradesh
- 30+ new policies
- 20+ new events
- Bug fixes

### v2.0.0 (Major Update)

- Achievements system
- Leaderboards
- Challenge modes
- 2 additional states

---

## Test Coverage Targets

| Phase | Coverage Target | Focus Areas |
|-------|-----------------|-------------|
| Phase 1 | 70% | Core loop, save/load |
| Phase 2 | 85% | All game systems |
| Phase 3 | 80% | Data validation |
| Phase 4 | 60% | UI interactions |
| Phase 5 | 90% | Localization keys |
| Phase 6 | 75% | Integration mocks |
| Phase 7 | N/A | Performance benchmarks |
| Phase 8 | 100% | Full regression |

---

## Key Milestones

| Milestone | Definition of Done |
|-----------|-------------------|
| **Alpha** | Core loop playable, placeholder content, **Phase 1-2 tests passing** |
| **Beta** | Full Telangana content, all UI complete, **Phase 1-6 tests passing** |
| **Release Candidate** | Bug-free, balanced, store-ready, **All tests passing** |
| **Launch** | Live on Google Play, **Smoke tests gate releases** |

---

## Technical Caveats

| Risk | Mitigation |
|------|------------|
| Unity learning curve | Budget extra time if new to Unity |
| SQLite on mobile | Test on real devices early, not just emulator |
| Telugu text rendering | Test fonts early, may need custom font asset |
| Balance tuning | Plan for multiple iteration cycles |
| Store approval | Follow Google Play policies strictly |
| Test flakiness | Use deterministic seeds for random tests |

---

## Getting Started

### Immediate Next Steps

1. **Set up Unity project** - Create new 2D project with recommended settings
2. **Set up Unity Test Framework** - Create EditMode and PlayMode test assemblies
3. **Import SQLite plugin** - Get SQLite4Unity3d from Asset Store
4. **Create basic database schema** - Implement tables from architecture doc
5. **Build turn system prototype** - Basic turn advancement with UI
6. **Write first tests** - TurnManager and GameState tests
7. **Test with 3-5 dummy policies** - Verify system works end-to-end

### Development Environment

| Tool | Version |
|------|---------|
| Unity | 2023.2 LTS |
| Unity Test Framework | 1.3+ |
| Visual Studio / Rider | Latest |
| Android SDK | API 34 |
| SQLite4Unity3d | Latest |
| Google Play Games Plugin | v0.11+ |
