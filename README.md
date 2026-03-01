# Nene CM - Political Strategy Game

A fully local, turn-based political strategy mobile game where players take on the role of Chief Minister of Indian states. Built with Unity for Android/iOS.

## Overview

**Nene CM** (Telugu: "I am the CM") is a data-driven political simulation game focused on real Indian state policies. Players manage budgets, implement policies, and maintain public approval to win re-election.

### Key Features

- **Turn-based gameplay** - Each turn represents one month of governance
- **Real policies** - Based on actual government schemes (Rythu Bandhu, etc.)
- **Multiple factions** - Balance rural farmers, urban professionals, students, and more
- **Election system** - Face re-election every 5 years (60 turns)
- **Fully offline** - No internet required after download
- **Bilingual** - English and Telugu language support

## Tech Stack

| Component | Technology |
|-----------|------------|
| Game Engine | Unity 2023.2 LTS |
| Language | C# |
| Database | SQLite (local) |
| Cloud Saves | Google Play Games |
| Analytics | Firebase Analytics |
| Monetization | Unity IAP (₹79 premium unlock) |
| Platform | Android (iOS planned) |

## Architecture Overview

```
┌─────────────────────────────────────────┐
│         UNITY GAME ENGINE               │
│  (C# scripting, cross-platform build)   │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│        DATA LAYER (SQLite)              │
│  • Policies database                    │
│  • Events & scenarios                   │
│  • Game state & saves                   │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│       SERVICES (FREE SDKs)              │
│  • Google Play Games (cloud save)       │
│  • Firebase Analytics (metrics)         │
│  • Unity IAP (in-app purchase)          │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│      PLATFORM (Android/iOS)             │
└─────────────────────────────────────────┘
```

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, TurnManager, StateManager, EventBus
│   ├── Data/           # PolicyData, GameState, DatabaseManager
│   ├── Systems/        # BudgetSystem, ApprovalSystem, PolicySystem, EventSystem
│   ├── UI/             # MainMenu, GameplayUI, PolicyCardUI, DashboardUI
│   └── Utilities/      # JSONHelper, MathHelper
├── Data/
│   ├── Policies/       # State-specific policy JSON files
│   └── Database/       # SQLite database files
├── UI/
│   ├── Prefabs/        # Reusable UI components
│   └── Sprites/        # Icons, backgrounds
├── StreamingAssets/    # Files copied to device
└── Plugins/
    ├── SQLite/         # SQLite plugin
    └── PlayGames/      # Google Play Games SDK
```

## Business Model

- **Free version**: Play Term 1 (5 years / 60 turns) completely free
- **Premium unlock**: ₹79 one-time purchase for unlimited terms
- **Zero operational costs**: Local-first architecture, no servers needed

## Documentation

- [Architecture Guide](docs/ARCHITECTURE.md) - Detailed technical architecture
- [Development Roadmap](docs/ROADMAP.md) - Implementation phases and timeline

## Quick Start

1. Install Unity 2023.2 LTS
2. Clone this repository
3. Import SQLite plugin from Asset Store
4. Open project in Unity
5. Build for Android

## Target Specifications

| Metric | Target |
|--------|--------|
| APK Size | 50-70 MB |
| Min Android | API 24 (Android 7.0) |
| Target Android | API 34 (Android 14) |
| Offline Support | 100% |
| Languages | English, Telugu |

## License

Proprietary - All rights reserved
