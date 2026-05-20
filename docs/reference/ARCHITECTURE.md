# System Architecture

> **Purpose**: stable reference for how the sonification system is shaped, what is in the codebase, who owns what, and where to make future changes. Not a sprint plan — for the v1 sprint plan see `../plans/PLAN_2026_05_21.md`.

> **Audio stack**: Native Unity (AudioSource + AudioMixer). Reasoning in `REPORT.md`.

## 1. Team and Responsibilities

The project team is **2 code developers + 2 audio designers**. The audio team works in a DAW and delivers audio files; the code team owns everything inside the Unity project. This split is what shaped the architecture below.

| Team | Owns | Deliverable |
|------|------|-------------|
| Audio team (2 people) | Sound design, recording, editing, mastering. Musical content decisions (chord choice, voicing, timbre). | Audio files in `./sounds/` |
| Code team (2 people)  | Unity project, sonification pipeline, parameter exposure, wiring audio files into the project | C# code in `Assets/Sonification/`, the `ChordPalette` and `SonificationConfig` ScriptableObjects |

**Inter-team contract**: file-naming convention `{Chord}_{NoteIndex}.mp3` (e.g., `C_1.mp3`, `F_2.mp3`). The audio team can replace any file in place; if the name stays the same, no code change is needed.

## 2. Repository Layout

```
GCT-Final-Project-Game-of-Life/
├── README.md                        ← entry point, navigation
├── Assets/                          ← Unity project code
│   ├── GameManager.cs               ← Game of Life simulation
│   ├── GridRenderer.cs              ← editor-only grid gizmo
│   ├── Sonification/                ← sonification pipeline
│   │   ├── ChordPalette.cs
│   │   ├── SonificationConfig.cs
│   │   ├── GridAggregator.cs
│   │   ├── VoicePool.cs
│   │   └── Sonifier.cs
│   ├── Sounds/                      ← Unity-imported audio (mirror of ./sounds/)
│   ├── Scenes/, Settings/, TIle Assets/
├── sounds/                          ← source-of-truth audio (mp3, 9 files)
├── Packages/, ProjectSettings/, LICENSE
└── docs/                            ← all documentation
    ├── reference/                   ← stable references
    │   ├── ARCHITECTURE.md          ← this file
    │   ├── REPORT.md                ← audio stack decision rationale
    │   └── MPTK.md                  ← MIDI alternative (considered, not adopted)
    ├── guides/                      ← onboarding / how-to
    │   ├── HOW_TO_USE_UNITY.md      ← Unity setup walkthrough (EN)
    │   └── HOW_TO_USE_UNITY_KR.md   ← Unity setup walkthrough (KR)
    ├── plans/                       ← dated sprint plans (append-only)
    │   └── PLAN_2026_05_21.md
    ├── log/
    │   └── LOG.md                   ← chronological action log (append-only)
    └── issues/
        └── ISSUE.md                 ← issue tracker (append-only)
```

## 3. Sonification Pipeline (3 stages)

The sonification is split into **three replaceable stages** so that any one stage can be swapped without touching the others. The team expects to experiment with what row means and what column means; the architecture has to make those swaps cheap.

```
[Game of Life Grid state]
            │
            ▼
   ┌────────────────────┐
   │   Aggregator       │   produces: counts[chord, noteIndex]
   │ (knows the axis:   │   ← only this stage knows whether
   │  rows or columns)  │     we aggregate by row or by column
   └────────────────────┘
            │
            ▼
   ┌────────────────────┐
   │      Mapper        │   produces: (chord, noteIndex, volume) tuples
   │ (knows musical     │   ← only this stage knows the
   │  rules: row%len,   │     row-to-note rule and the
   │  loudness curve)   │     loudness normalization
   └────────────────────┘
            │
            ▼
   ┌────────────────────┐
   │   Player           │   talks to Unity:
   │ (Voice Pool of 9   │   AudioSource[i].PlayOneShot(clip, volume)
   │  AudioSources)     │   ← re-triggers per N ticks; only this
   │                    │     stage touches AudioSource
   └────────────────────┘
```

**The contract between stages is plain data** (an integer count array, a list of volume tuples). Stages do not depend on each other's internals.

**What this buys us:**
- Swap "row count" for "column count" → edit Aggregator only.
- Try a logarithmic loudness curve → edit Mapper only.
- Replace mp3 with MIDI later → edit Player only.

## 4. Color → Chord Mapping

The grid uses three cell colors based on neighbor count. Each color maps to one of three chords (C / F / G major, the I-IV-V of C major — mutually consonant by construction, so no combination produces dissonance).

| Cell state                       | Tile field            | Color  | Chord   |
|----------------------------------|-----------------------|--------|---------|
| Alive, exactly 2 neighbors       | `alive2NeighborsTile` | Green  | C major |
| Alive, exactly 3 neighbors       | `alive3NeighborsTile` | Blue   | F major |
| Alive, any other neighbor count  | `aliveOtherTile`      | Red    | G major |

The classification lives in `GameOfLifeManager.GetChordIndex(int i, int j)` — returns -1 for dead cells, otherwise 0 / 1 / 2 for C / F / G. This mirrors the visual tile classification in `UpdateCellTile`, so audio voice and visual color are guaranteed to agree.

## 5. Files in the Codebase

| Path | Role |
|------|------|
| `Assets/GameManager.cs` | Game of Life simulation. Owns the `cells[]` array, calls `Sonifier.OnTick()` at the end of each generation. Exposes `GridSize` and `GetChordIndex` for the sonification layer. |
| `Assets/GridRenderer.cs` | Editor-only grid gizmo. No runtime effect. |
| `Assets/Sounds/` | Unity-imported audio assets (mirror of `./sounds/`). Unity only loads audio from inside `Assets/`. |
| `Assets/Sonification/ChordPalette.cs` | `ScriptableObject`. Holds the chord array; each chord has a label and an `AudioClip[]`. Editable in Inspector via `Create → Sonification → Chord Palette`. |
| `Assets/Sonification/SonificationConfig.cs` | `ScriptableObject`. Holds tunable parameters: `triggerEveryNTicks`, `loudnessSaturationCount`, `minimumAudibleVolume`. |
| `Assets/Sonification/GridAggregator.cs` | Static. Reads `cells[]` via the `GameOfLifeManager` reference and produces `int[chord, noteIndex]` count table. The row-vs-column axis lives in one line inside this file. |
| `Assets/Sonification/VoicePool.cs` | `MonoBehaviour`. Owns 9 child `AudioSource` GameObjects (one per chord × note). Exposes `Trigger(chord, note, volume)` → calls `PlayOneShot`. Overlapping triggers mix as decaying tails. |
| `Assets/Sonification/Sonifier.cs` | `MonoBehaviour` glue. Wires Aggregator → Mapper → VoicePool. Called by `GameManager` once per tick. Holds Inspector references to all four collaborators. |

**Note on `./sounds/` vs `Assets/Sounds/`**: Unity will not load audio from outside the `Assets/` folder. The repo-root `./sounds/` directory is the **source-of-truth copy** that lives in version control alongside docs; the `Assets/Sounds/` copy is what Unity actually imports. If you add or replace a clip, update both.

## 6. How a Collaborator Modifies Things Later

| Change you want to make                       | Where to edit                          |
|-----------------------------------------------|----------------------------------------|
| Swap row aggregation for column aggregation   | `GridAggregator.cs` only               |
| Change which color maps to which chord        | `ChordPalette` asset (Inspector)       |
| Change the chord clips                        | Replace files in `Assets/Sounds/`, reassign in `ChordPalette` |
| Change the loudness curve (linear → log)      | `Sonifier.cs` mapping function         |
| Tune loudness threshold or trigger cadence    | `SonificationConfig` asset (Inspector) |
| Add a fourth color and chord                  | Add a tile field in `GameManager.cs`, extend `GetChordIndex`, add an entry in `ChordPalette`. `VoicePool` resizes from palette length. |

The principle: **prefer editing assets (ScriptableObjects) over editing code** wherever possible. Code edits should be reserved for changes that affect the pipeline structure itself.

## 7. Documentation Workflow

The team's docs follow a few simple rules so they stay useful as the project iterates:

- **`docs/reference/`** — long-lived. Update in place when the system changes.
- **`docs/guides/`** — long-lived. Update when the setup steps change.
- **`docs/plans/`** — append-only. Each iteration creates a new `PLAN_YYYY_MM_DD.md`. Do not mutate old plan files — they are frozen snapshots that record what we intended at a given time.
- **`docs/log/LOG.md`** — append-only. Record actions as they happen (what was done, why, which files), not after the fact.
- **`docs/issues/ISSUE.md`** — append-only. New issues appended at the bottom. When an issue is resolved, move it to the "Resolved" section and record the chosen outcome.

The iteration loop is: **issue discovered → add to `ISSUE.md` → pick one to resolve → write a `PLAN_YYYY_MM_DD.md` for the work → execute → append each action to `LOG.md` → close the issue with its resolution**.

## 8. Handover Notes for Collaborators

- The `./sounds/` directory at the repository root is the **source of truth** for audio assets. `Assets/Sounds/` is the Unity import copy. When adding or replacing audio, update both locations.
- **Do not put sonification logic inside `GameManager.cs`.** Keep the simulation and the audio in separate files so each can be reasoned about independently. `GameManager` should call into `Sonifier` once per tick and otherwise not know audio exists.
- When making a music-theory judgment call, defaulting to the C / F / G major triads we already use is safe — they are mutually consonant by construction and cannot clash regardless of how the simulation evolves.
- `../log/LOG.md` records *what was actually done and when*. Update it as you work, not after the fact, so a collaborator joining mid-project can follow the history.
- `../issues/ISSUE.md` records open issues and proposed options. Decisions get recorded both there (Resolution) and in `LOG.md` (action taken).
- New iteration plans go in `../plans/PLAN_YYYY_MM_DD.md`. Don't mutate old plan files — they are frozen snapshots.
