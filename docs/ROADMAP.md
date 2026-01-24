# Development Roadmap

Implementation phases for the Nene CM political strategy game.

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

### Deliverables

- Working turn system prototype
- Save/load functionality
- Basic navigation between screens

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

### Deliverables

- All core systems functional
- Systems communicating via EventBus
- Test with placeholder data

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

### Deliverables

- Complete Telangana database
- Playable from start to first election
- Initial balance pass

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

### Deliverables

- Complete UI for all screens
- Smooth animations
- Audio feedback

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

### Deliverables

- Full Telugu translation
- Language switching works correctly
- No text overflow issues

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

### Deliverables

- Cloud saves working
- Premium purchase working
- Analytics dashboard configured

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

### Deliverables

- 60 FPS on mid-range devices
- < 3 second load time
- Store-ready assets

---

## Phase 8: Testing

**Focus:** Quality assurance

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

### Deliverables

- Stable, bug-free build
- Balanced gameplay
- Positive beta feedback

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

### Deliverables

- Live on Google Play Store
- Positive reviews
- Healthy retention metrics

---

## Future Phases (Post-Launch)

### Phase 10: Additional States

- [ ] Andhra Pradesh content pack
- [ ] Karnataka content pack
- [ ] Tamil Nadu content pack
- [ ] Maharashtra content pack

### Phase 11: iOS Release

- [ ] iOS build configuration
- [ ] Apple Sign-In integration
- [ ] Game Center integration
- [ ] App Store submission

### Phase 12: Advanced Features

- [ ] Achievements system
- [ ] Leaderboards
- [ ] Challenge modes
- [ ] Multiplayer (competing CMs)

---

## Key Milestones

| Milestone | Definition of Done |
|-----------|-------------------|
| **Alpha** | Core loop playable, placeholder content |
| **Beta** | Full Telangana content, all UI complete |
| **Release Candidate** | Bug-free, balanced, store-ready |
| **Launch** | Live on Google Play |

---

## Technical Caveats

| Risk | Mitigation |
|------|------------|
| Unity learning curve | Budget extra time if new to Unity |
| SQLite on mobile | Test on real devices early, not just emulator |
| Telugu text rendering | Test fonts early, may need custom font asset |
| Balance tuning | Plan for multiple iteration cycles |
| Store approval | Follow Google Play policies strictly |

---

## Getting Started

### Immediate Next Steps

1. **Set up Unity project** - Create new 2D project with recommended settings
2. **Import SQLite plugin** - Get SQLite4Unity3d from Asset Store
3. **Create basic database schema** - Implement tables from architecture doc
4. **Build turn system prototype** - Basic turn advancement with UI
5. **Test with 3-5 dummy policies** - Verify system works end-to-end

### Development Environment

| Tool | Version |
|------|---------|
| Unity | 2023.2 LTS |
| Visual Studio / Rider | Latest |
| Android SDK | API 34 |
| SQLite4Unity3d | Latest |
| Google Play Games Plugin | v0.11+ |
