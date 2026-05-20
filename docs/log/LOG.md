# Action Log

> Chronological record of decisions and changes. Append new entries at the bottom. Each entry: **Purpose** (why), **Action** (what was done), **Files touched** (where to look), **Notes** (anything a future reader needs to know).

---

## 2026-05-20

### Defined the sonification concept

**Purpose**: Frame the audio extension of the existing Game of Life simulation before writing any code.

**Action**: Agreed on the mapping
- color → chord
- row → note (row % chord length)
- per-row count of same-color cells → loudness of that (chord, note) voice.

Decided to start with 3 chords (C / F / G major, the I-IV-V of C major) and reduce the existing 4 cell colors to 3 to match. Selected I-IV-V because the three chords are mutually consonant under any combination, so the sonification cannot produce dissonance regardless of how the simulation evolves.

**Files touched**: None yet (concept-only).

---

## 2026-05-21

### Wrote initial architecture docs

**Purpose**: Capture the audio-stack decision and implementation plan before writing code, so the 4-person team can align without re-litigating choices.

**Action**: Wrote three documents.
- `REPORT.md` — surveys audio-stack options, explains the chosen one. Initial version assumed solo dev and rejected Wwise; later revised (see entry below).
- `PLAN.md` — directory layout, 3-stage pipeline (Aggregator → Mapper → Player), color reduction edits, file-by-file plan, ordered implementation steps, modification guide for collaborators.
- `MPTK.md` — informational doc on the MIDI + Soundfont alternative (Maestro MPTK). Not adopted for v1; documented so the team is aware of it for future iterations.

**Files touched**: `REPORT.md`, `PLAN.md`, `MPTK.md`.

### Revisited the Wwise decision after team-structure was clarified

**Purpose**: The initial REPORT was written assuming a solo developer. The actual team is 2 code + 2 audio designers, which changes the calculus for middleware.

**Action**: Rewrote `REPORT.md` to evaluate Wwise honestly against the real team structure. Outcome: still Native Unity, but for a different reason — the audio team chose a DAW → file-delivery workflow (no in-engine ownership), which removes Wwise's central value proposition. The decision is now framed as deliberate, not as "Wwise is overkill."

**Files touched**: `REPORT.md` (rewritten), `PLAN.md` (added §1 Team and Responsibilities, renumbered following sections).

### Pivoted from looping drone to per-tick re-triggering

**Purpose**: Verified the actual mp3 length and properties via `afinfo`. Discovered that mp3 has built-in priming (~15 ms) and remainder (~60 ms) samples that prevent gapless looping — a click/gap would be audible every 4 seconds in a looped-drone setup. Also clarified with the user that the desired aesthetic is "the simulation is playing music" rather than ambient drone.

**Action**: Switched the Player stage from `loop=true` + volume modulation to `PlayOneShot` per N ticks with per-strike volume. This both fixes the mp3 looping artifact (no looping = no problem) and matches the intended musical feel. Updated docs accordingly.

**Files touched**: `REPORT.md` §6, `PLAN.md` §3 (diagram), §5 (file roles), §6 (impl steps), §8 (open decisions resolved).

### Reduced cell colors from 4 to 3

**Purpose**: Make the visual palette match the 3-chord audio palette. 4-neighbor case folds into the catch-all "other" bucket.

**Action**: Edited `Assets/GameManager.cs`:
- Removed the `alive4NeighborsTile` field declaration.
- Removed the `else if (neighbors == 4) tile = alive4NeighborsTile;` branch from `UpdateCellTile`.
- Updated comments on the remaining tile fields to record the color→chord mapping.

**Files touched**: `Assets/GameManager.cs`.

**Notes for the team**: in the Unity Inspector, the `alive4NeighborsTile` slot will disappear from the GameManager component on next domain reload. No scene cleanup needed.

### Wired GameManager to the new sonification pipeline

**Purpose**: Let the simulation drive audio without putting audio logic into `GameManager`.

**Action**: Added to `Assets/GameManager.cs`:
- `[SerializeField] private Sonifier sonifier;` field (Inspector-assigned; null is safe — simulation runs silent).
- `public int GridSize => gridSize;` accessor.
- `public int GetChordIndex(int i, int j)` method. Returns -1 for dead cells, otherwise 0/1/2 for C/F/G based on neighbor count. Mirrors the visual tile classification, so audio voice and visual color are guaranteed to agree.
- One-line call `if (sonifier != null) sonifier.OnTick();` at the end of `NextGeneration()`.

**Files touched**: `Assets/GameManager.cs`.

### Created Sonification code files

**Purpose**: Implement the 3-stage pipeline as planned.

**Action**: Created `Assets/Sonification/` with:
- `ChordPalette.cs` — `ScriptableObject`. Holds the chord array; each chord has a label and an `AudioClip[]`. `[CreateAssetMenu]` exposes "Create → Sonification → Chord Palette" in the Project window.
- `SonificationConfig.cs` — `ScriptableObject`. Holds `triggerEveryNTicks`, `loudnessSaturationCount`, `minimumAudibleVolume`. `[CreateAssetMenu]` exposes "Create → Sonification → Config".
- `GridAggregator.cs` — static class. `Aggregate(game, palette)` walks every alive cell, classifies via `game.GetChordIndex`, and returns `int[chord, noteIndex]`. The row-vs-column axis lives in one line inside this file; swapping to column aggregation is a 1-line edit.
- `VoicePool.cs` — `MonoBehaviour`. Builds 9 child GameObjects (one per chord×note) with AudioSources at `Awake`. Exposes `Trigger(chord, note, volume)` which calls `PlayOneShot`. Overlapping triggers mix naturally as decaying tails.
- `Sonifier.cs` — `MonoBehaviour`. Holds references to `GameOfLifeManager`, `ChordPalette`, `SonificationConfig`, `VoicePool`. `OnTick()` (called by `GameManager`) increments a tick counter, gates on `triggerEveryNTicks`, runs aggregation, normalizes count → volume, triggers voices above `minimumAudibleVolume`.

**Files touched**: `Assets/Sonification/{ChordPalette,SonificationConfig,GridAggregator,VoicePool,Sonifier}.cs` (5 new files).

### Imported audio assets into Unity

**Purpose**: Unity only loads audio from inside `Assets/`. The repo-root `./sounds/` is the source of truth; Unity needs its own copy.

**Action**: Copied 9 mp3 files from `./sounds/` to `Assets/Sounds/`. Unity will auto-generate `.meta` files on next editor open and import each file as an `AudioClip`.

**Files touched**: `Assets/Sounds/{C,F,G}_{1,2,3}.mp3` (9 new files).

**Notes for the team**: keep `./sounds/` as the canonical copy. When the audio team replaces a clip, drop the new file in `./sounds/` first, then copy to `Assets/Sounds/` (or just replace both at once).

---

## Manual Steps Remaining (for whoever opens Unity next)

The code and assets are in place. The following steps require the Unity editor and must be done by hand:

1. **Create `ChordPalette` asset**
   In the Project window: right-click → Create → Sonification → Chord Palette. Save it as `Assets/Sonification/MainChordPalette.asset` (any name works). Open it in the Inspector:
   - Set `Chords` array size to 3.
   - Element 0: label `C major`, `notes` size 3, drag in `C_1.mp3`, `C_2.mp3`, `C_3.mp3` from `Assets/Sounds/`.
   - Element 1: label `F major`, `notes` size 3, drag in `F_1`, `F_2`, `F_3`.
   - Element 2: label `G major`, `notes` size 3, drag in `G_1`, `G_2`, `G_3`.

   **Order matters**: index 0 = C, 1 = F, 2 = G must match `GameOfLifeManager.GetChordIndex` (C for 2-neighbors, F for 3-neighbors, G for everything else).

2. **Create `SonificationConfig` asset**
   Project window: Create → Sonification → Config. Save as `Assets/Sonification/MainSonificationConfig.asset`. Defaults are fine for the first test (`triggerEveryNTicks = 2`, `loudnessSaturationCount = 8`, `minimumAudibleVolume = 0.02`).

3. **Add Sonification GameObjects to the scene**
   In the scene's Hierarchy:
   - Create an empty GameObject named `VoicePool`. Add the `VoicePool` script. Assign the `ChordPalette` asset to its `Palette` field.
   - Create an empty GameObject named `Sonifier`. Add the `Sonifier` script. Assign:
     - `Game` → the existing GameObject that has `GameOfLifeManager`.
     - `Palette` → the same `ChordPalette` asset.
     - `Config` → the `SonificationConfig` asset.
     - `Voice Pool` → the `VoicePool` GameObject.

4. **Wire Sonifier into GameOfLifeManager**
   Select the GameObject with `GameOfLifeManager`. In its Inspector, drag the `Sonifier` GameObject into the new `Sonifier` slot under the `Sonification` header.

5. **Test**
   Press Play. Press Space to unpause. Draw some cells with the mouse if the grid starts empty. You should hear chord notes triggering every 2 ticks with volume proportional to row-wise cell density per color.

**Tuning hints**:
- If notes overlap too densely → raise `triggerEveryNTicks` to 3 or 4, or slow GoL with the `S` key.
- If everything sounds equally loud → raise `loudnessSaturationCount` (so it takes more cells to reach max volume).
- If sparse grids still produce a wash of quiet notes → raise `minimumAudibleVolume` to 0.05 or 0.1.

> The "Manual Steps Remaining" section above is preserved as a historical snapshot from when the v1 code was committed. Those steps have since been executed (see entry below) and the canonical version is now in `../guides/HOW_TO_USE_UNITY.md`.

---

## 2026-05-21 (continued)

### Wrote Unity setup walkthrough (EN + KR)

**Purpose**: The "Manual Steps Remaining" notes above were too terse for a teammate who has never opened Unity. Wrote a beginner-friendly walkthrough.

**Action**: Wrote `HOW_TO_USE_UNITY.md` (English) and `HOW_TO_USE_UNITY_KR.md` (Korean). 8-step walkthrough covering panel orientation, asset creation, GameObject creation, slot wiring, audio output check, play/test, troubleshooting table. Both versions cross-reference each other.

**Files touched**: `HOW_TO_USE_UNITY.md` (created), `HOW_TO_USE_UNITY_KR.md` (created). *(Both later moved to `docs/guides/` during the documentation restructure — see below.)*

### Completed Unity manual wiring (user-executed)

**Purpose**: Get the sonification running end-to-end inside Unity.

**Action**: User followed `HOW_TO_USE_UNITY.md` step by step:
- Created `MainChordPalette` asset with C/F/G chord entries × 3 clips each.
- Created `MainSonificationConfig` asset with default values.
- Created `VoicePool` and `Sonifier` GameObjects in the scene; added respective components.
- Wired Sonifier's four reference slots (`Game` → `GameManager`, `Palette` → `MainChordPalette`, `Config` → `MainSonificationConfig`, `Voice Pool` → `VoicePool`).
- Wired `GameManager`'s `Sonifier` slot to point at the `Sonifier` GameObject.
- Hit Play. Audio confirmed working: chord notes trigger as the simulation evolves.

**Notes for the team**: Common confusion point caught and documented — the GameObject in the scene is named `GameManager`, but the script attached is `GameOfLifeManager`. Unity matches by script class, not GameObject name, so the drag-drop works. `HOW_TO_USE_UNITY.md` §1 now calls this out explicitly.

### Opened ISSUE-001 — Sonification feels static

**Purpose**: After listening to a recorded play session, identified that the audio does not feel responsive to simulation motion. Documented before picking a fix so the team can choose direction together.

**Action**: Recorded ~46s of play (`/Users/kotmul2no/Downloads/무제.mov`), extracted RMS audio levels via `ffmpeg astats`, confirmed audio sits in a narrow 12 dB band (-52 to -64 dB) consistent with "constant background pad." Also inspected a mid-video frame — grid was dominated by one chord (blue/3-neighbor), so harmonic motion is absent too. Wrote up symptom, evidence, diagnosis (three independent causes), and three proposed options (A: tuning, B: top-K trigger, C: change-based trigger) with effort/effect estimates and a recommendation.

**Files touched**: `ISSUE.md` (created). *(Later moved to `docs/issues/`.)*

### Restructured documentation into `docs/` folder

**Purpose**: As the project iterates, the number of markdown files at the repo root was growing fast (REPORT, PLAN, MPTK, HOW_TO_USE_UNITY ×2, LOG, ISSUE). The team agreed on an iterative workflow (Issue → dated PLAN → execute → LOG entry → resolve issue) and asked to give the docs a folder structure that scales.

Also noted: the original `PLAN.md` was doing two jobs — it contained both stable architecture reference AND v1 sprint-specific implementation steps. Split into two files so each has one clear purpose.

**Action**:
- Created `docs/` with subfolders `reference/`, `guides/`, `plans/`, `log/`, `issues/`.
- Moved `REPORT.md` and `MPTK.md` → `docs/reference/`.
- Moved `HOW_TO_USE_UNITY.md` and `HOW_TO_USE_UNITY_KR.md` → `docs/guides/`.
- Moved `LOG.md` → `docs/log/`.
- Moved `ISSUE.md` → `docs/issues/`.
- Split old `PLAN.md`:
  - Stable parts (team roles, repo layout, 3-stage pipeline, color mapping, file inventory, modification guide, handover notes, NEW workflow section) → `docs/reference/ARCHITECTURE.md`.
  - Sprint-specific parts (implementation steps, open decisions as-of-date) → `docs/plans/PLAN_2026_05_21.md`, marked as frozen.
- Deleted the original `PLAN.md`.
- Created `README.md` at the repo root as the entry point: 1-line project description, links to Unity setup guides, workflow diagram, documentation map.
- Updated cross-references in moved files to point to the new locations (and to `ARCHITECTURE.md` where they previously pointed at `PLAN.md`'s architecture sections).
- Historical references to old filenames in this LOG file were left intact — they describe past state accurately.

**Files touched**: created `README.md`, `docs/reference/ARCHITECTURE.md`, `docs/plans/PLAN_2026_05_21.md`. Moved 6 md files into `docs/` subfolders. Edited cross-references in `docs/reference/REPORT.md`, `docs/reference/MPTK.md`, `docs/guides/HOW_TO_USE_UNITY.md`, `docs/issues/ISSUE.md`. Deleted original `PLAN.md`.

**Notes for the team**: Going forward, the iteration loop is documented in `docs/reference/ARCHITECTURE.md` §7 and in `README.md`. New issues → `docs/issues/ISSUE.md`; resolving an issue → write a new `docs/plans/PLAN_YYYY_MM_DD.md`, execute, and append actions to this LOG. Old plan files are frozen — do not edit them in place; write a new dated plan instead.
