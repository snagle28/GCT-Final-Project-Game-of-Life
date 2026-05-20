# Issues

> Project-level issues that need discussion or a decision. Each issue is dated, describes the symptom, the evidence collected, the diagnosis, and the proposed options. Append new issues at the bottom. Move resolved issues to the "Resolved" section at the very bottom along with the chosen outcome.

---

## ISSUE-001 — Sonification feels static; sound does not respond to simulation motion

**Opened**: 2026-05-21
**Status**: Open — awaiting decision on direction
**Files involved**: `Assets/Sonification/Sonifier.cs`, `Assets/Sonification/GridAggregator.cs`, `MainSonificationConfig` asset

### Symptom

After completing the v1 sonification (see `../reference/ARCHITECTURE.md`, `../plans/PLAN_2026_05_21.md`, `../log/LOG.md`), playback works — chord notes do trigger and audio plays at the expected pulse. However, listening to a recorded play session, the **audio does not feel like it is changing as the simulation evolves**. The texture is largely constant. The intended "the Game of Life is performing music as it moves" aesthetic is not landing.

### Evidence

**1. Audio dynamic range is unusually narrow.**
RMS audio level (`ffmpeg astats`) across a 46-second recording stays in a tight ~12 dB band:

```
Most frames:  -52 dB  to  -64 dB
Musical reference: dynamic pieces typically span 20-40 dB
```

A 12 dB spread is consistent with "background pad that is always playing at a similar level" rather than "music that ebbs and flows."

**2. One chord dominates the visual at any given moment.**
A mid-video frame inspection shows the grid is dominated by 3-neighbor cells (blue), with very few 2-neighbor (yellow) cells and effectively no "other" (red) cells. This means **only one of the three chords has meaningful volume in that frame** — there is no chord-level harmonic motion either.

### Diagnosis

The current pipeline averages variation out by design. Three independent factors compound:

**1. `row % chordLength` aggregation collapses 50 rows into 3 slots.**
For a chord with 3 notes and a 50-row grid, ~17 rows feed each note slot. Local changes (a cell appears here, disappears there) get summed into a per-slot count that barely moves. The signal that *should* drive musical motion is averaged away before reaching the audio.

**2. Saturation threshold is too low (`loudnessSaturationCount = 8`).**
With ~17 rows feeding each slot and typical GoL densities, almost every slot's count exceeds 8 most of the time, so volumes are pinned near 1.0. Count differences above the threshold are invisible — the dynamic range is squashed at the top.

**3. Every trigger fires all 9 voices.**
The current `Sonifier.OnTick` walks all (chord, note) pairs and triggers each one whose volume passes `minimumAudibleVolume`. With saturated counts, this means **all 9 voices fire on every trigger at near-max volume**. The result is a constant chord-pad texture instead of music that breathes.

These factors are independent. Fixing only one will help partially; the strongest improvements come from changing the triggering strategy (factor 3), because that directly addresses "the simulation is playing music" rather than "the simulation is driving a static drone."

### Proposed Options

#### Option A — Tuning only (no code changes)

- In `MainSonificationConfig` (Unity Inspector):
  - `Loudness Saturation Count`: 8 → **40**
  - `Trigger Every N Ticks`: 2 → **4**
  - `Minimum Audible Volume`: 0.02 → **0.1**
- Optionally slow the simulation (`S` key in Play mode) to give the ear time to register changes.

**Effort**: 30 seconds, no code.
**Expected effect**: Modest. Spreads volumes over more of the 0..1 range and reduces trigger density. Still does not fix factor 3 (all voices fire every trigger).

#### Option B — Top-K trigger (adds melodic shape)

In `Sonifier.OnTick`, after computing volumes, sort the (chord, note) candidates by volume and trigger only the top **K** (suggested K = 2 or 3). Voices not triggered this tick continue to ring out from their previous trigger.

- New config field: `int topKVoicesPerTrigger = 3`.
- Code change: ~10 lines in `Sonifier.cs`.

**Effort**: 5 minutes.
**Expected effect**: Significant. The loudest contributions become foreground "melody" while quieter ones provide background sustain via their decaying tails. The pad becomes a melodic line.

#### Option C — Change-based trigger (most aligned with the stated aesthetic) — RECOMMENDED

Trigger only the voices whose count has changed meaningfully since the previous evaluation. Stable patterns produce near-silence (only the natural decay of prior triggers); evolving patterns produce bursts of new triggers proportional to how much the simulation is moving.

- Sonifier stores `int[,] previousCounts` between ticks.
- For each (chord, note), compute `|count - previousCount|`. If above a threshold, trigger.
- New config field: `int changeThreshold = 2` (smallest count delta that earns a re-trigger).
- Code change: ~15 lines in `Sonifier.cs`.

**Effort**: 5–10 minutes.
**Expected effect**: Largest. The audio directly tracks the *amount of motion* in the simulation, which is exactly what the user's complaint described missing. Pattern stabilizations become musical rests; pattern transitions become musical phrases.

### Open Questions

- Are Options B and C complementary? (Yes, they can be combined: change-based trigger AND limit to top K of the changed voices. Worth considering as a v3 if v2 still feels off.)
- Should the change threshold scale with grid size? (Probably yes for portability, but not urgent for the 50×50 default.)
- Does adopting Option C make the existing `Trigger Every N Ticks` parameter redundant? (No — N still controls the polling cadence; change-based decides *whether* to fire on that cadence.)

### Recommendation

Try Option **A** first as a sanity check (30 seconds, no risk). If the feel is still off, implement Option **C** — it most directly addresses the stated complaint and the code change is small and localized to `Sonifier.cs`. Option B is a strong backup if C alone is not enough.

### Resolution

*(Fill in when decided.)*

---

## Resolved

*(Move issues here when closed, with the chosen outcome.)*
