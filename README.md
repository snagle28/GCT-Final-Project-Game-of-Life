# GCT Final Project — Game of Life × Sonification

A Unity implementation of Conway's Game of Life that **plays music as it evolves**: cell color maps to a musical chord, grid row maps to a note within that chord, and the count of same-color cells in each row controls how loudly that note sounds.

**Team**: 2 code developers + 2 audio designers.

## Get Started

If you just want to run the project and hear it:
- **English**: [`docs/guides/HOW_TO_USE_UNITY.md`](docs/guides/HOW_TO_USE_UNITY.md)
- **한국어**: [`docs/guides/HOW_TO_USE_UNITY_KR.md`](docs/guides/HOW_TO_USE_UNITY_KR.md)

These walk you through Unity setup step by step (no prior Unity experience assumed).

## How the Project Iterates

The project is iterative. Issues are surfaced, picked, planned, and resolved on a loop:

```
  discover problem
       │
       ▼
  add to ISSUE.md ──► pick one ──► write PLAN_YYYY_MM_DD.md
                                          │
                                          ▼
                                    execute the plan
                                          │
                                          ▼
                                  append each action
                                     to LOG.md
                                          │
                                          ▼
                              close issue with Resolution
```

Three append-only artifacts capture the history:

| Artifact | Lives at | Append rule |
|----------|----------|-------------|
| Issues | [`docs/issues/ISSUE.md`](docs/issues/ISSUE.md) | New issue at the bottom. When resolved, move it to the "Resolved" section with the chosen outcome. |
| Plans | [`docs/plans/`](docs/plans/) | Each iteration creates a new `PLAN_YYYY_MM_DD.md`. Do not edit old plan files — they are frozen snapshots. |
| Action log | [`docs/log/LOG.md`](docs/log/LOG.md) | Record actions as they happen (purpose, action, files touched), not after the fact. |

## Documentation Map

```
docs/
├── reference/                  ← stable references (update in place)
│   ├── ARCHITECTURE.md         ← system architecture, file inventory, modification guide
│   ├── REPORT.md               ← why this audio stack was chosen (vs Wwise etc.)
│   └── MPTK.md                 ← MIDI alternative we considered but did not adopt
├── guides/                     ← onboarding (update in place)
│   ├── HOW_TO_USE_UNITY.md     ← Unity setup walkthrough (EN)
│   └── HOW_TO_USE_UNITY_KR.md  ← Unity setup walkthrough (KR)
├── plans/                      ← dated sprint plans (append-only)
│   └── PLAN_2026_05_21.md
├── log/                        ← chronological action log (append-only)
│   └── LOG.md
└── issues/                     ← issue tracker (append-only)
    └── ISSUE.md
```

**Where to start reading:**
- **New to the project?** → [`HOW_TO_USE_UNITY.md`](docs/guides/HOW_TO_USE_UNITY.md), then [`ARCHITECTURE.md`](docs/reference/ARCHITECTURE.md).
- **Want to know why we built it this way?** → [`REPORT.md`](docs/reference/REPORT.md).
- **Want to know what's currently broken / open?** → [`ISSUE.md`](docs/issues/ISSUE.md).
- **Want to follow the history?** → [`LOG.md`](docs/log/LOG.md).

## Repository Layout (top level)

```
GCT-Final-Project-Game-of-Life/
├── README.md                ← this file
├── Assets/                  ← Unity project (C# code, scenes, tile assets)
│   ├── GameManager.cs       ← Game of Life simulation
│   ├── Sonification/        ← sonification pipeline (5 files)
│   └── Sounds/              ← Unity-imported audio
├── sounds/                  ← source-of-truth audio (mp3, 9 files)
├── Packages/, ProjectSettings/, LICENSE
└── docs/                    ← all documentation (see map above)
```
