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

## Future Phases (Post-Launch)

### Phase 10: Additional States

- [ ] Andhra Pradesh content pack
- [ ] Karnataka content pack
- [ ] Tamil Nadu content pack
- [ ] Maharashtra content pack

**Tests for each state:**
- [ ] Run all DataValidationTests with new content
- [ ] Run BalanceTests for new state
- [ ] Regression test: existing states unaffected

### Phase 11: iOS Release

- [ ] iOS build configuration
- [ ] Apple Sign-In integration
- [ ] Game Center integration
- [ ] App Store submission

**Tests:**
- [ ] All existing tests pass on iOS
- [ ] iOS-specific integration tests (Game Center, Sign-In)
- [ ] UI tests at iOS resolutions

### Phase 12: Advanced Features

- [ ] Achievements system
- [ ] Leaderboards
- [ ] Challenge modes
- [ ] Multiplayer (competing CMs)

**Tests:**
- [ ] Achievement unlock conditions
- [ ] Leaderboard score submission
- [ ] Challenge mode rule enforcement
- [ ] Multiplayer sync tests

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
