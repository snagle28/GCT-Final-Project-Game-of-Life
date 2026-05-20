# Visual-to-Sound Mapping: Approach Report

## 1. Context

This project extends a Unity Conway's Game of Life simulation with real-time sonification. The intended mapping is:

- **Color → Chord**: each cell color is assigned a musical chord.
- **Row → Note**: each grid row maps (modulo chord length) to one note within that chord.
- **Per-row count → Loudness**: the number of alive cells of a given color in a given row controls the loudness of the corresponding (chord, note) voice.

**Team structure**: 2 code developers + 2 audio designers (4 people total). The primary goal of the project is collaboration. This team composition is the decisive factor in the architecture choice below.

This document records the audio-stack options we considered, explains the reasoning, and states the chosen approach.

> A MIDI-based alternative (Maestro MPTK) is documented separately in `MPTK.md`. It is not in contention for this project because it does not fit a DAW-trained audio team's workflow.

## 2. Decision

**The project will use Native Unity audio (AudioSource + AudioMixer).** Wwise and other audio middleware are explicitly out of scope.

The reasoning is in §3 and §4. The implementation consequences of this choice are in §5.

## 3. Approaches Considered

### A. Native Unity (AudioSource + AudioMixer) — CHOSEN

- Audio team produces `wav` / `mp3` files in their DAW and hands them off.
- Code team imports the files into `Assets/`, wires them to `AudioSource` components in the Inspector, and modulates `.volume` from C# every Game-of-Life tick.
- All sound design happens *outside* the Unity project (in the DAW); all runtime control happens *inside* C# code.

**Strengths**
- Zero additional tooling, zero setup overhead.
- "Edit → press Play → hear result" loop with no intermediate build step.
- One language (C#) and one editor (Unity) for the runtime side.
- Anyone on the team can wire a new clip in 30 seconds.

**Weaknesses**
- Audio team's contribution is bounded at the file-delivery layer. They cannot iterate on mixing, dynamics, layering, or effect routing *inside the engine* without going through the code team.
- Every in-engine audio tweak becomes a code task.

### B. Audio Middleware (Wwise) — NOT CHOSEN

- Audio team would own a separate **Wwise project** in the Wwise Authoring application: sound design, Events, RTPCs (Real-Time Parameter Controls), bus mixing, effects.
- Wwise project is built into **SoundBanks** (binary blobs) loaded by Unity at runtime.
- Code team would call `AkSoundEngine.PostEvent(...)` and `SetRTPCValue(...)`. A clean Event/RTPC contract sits between the two teams.

**Why it would have been good (in a different scenario)**
- Audio team owns the entire audio stack end-to-end, with no code-team bottleneck.
- Industry-standard pipeline; carries portfolio/resume value.
- Built-in sample variation, ducking, mixing, profiler.

**Why it is wrong for this project (explained in §4)**
- The audio team does not want in-engine ownership of audio behavior — file delivery is the intended workflow. Wwise's central value proposition does not apply.
- Setup, learning curve, and SoundBank build pipeline would be paid for capabilities the team has chosen not to use.

## 4. Why Native Unity Is the Right Call Here

The decision came down to a single question, answered by the audio team:

> **Does the audio team want to own audio behavior in-engine — mixing, layering, dynamics, effects — or just deliver audio files?**

The answer is **deliver files**.

Once that is settled, Wwise's strengths collapse:

| Wwise's value proposition | Applies here? |
|---------------------------|---------------|
| Audio team owns in-engine audio behavior independently | **No** — audio team chose file-delivery workflow |
| Clean Event/RTPC contract scales with audio complexity | **No** — 9 fixed voices with simple volume modulation does not need scaling |
| Industry-standard sample variation, ducking, profiler | **No** — not features the team plans to use |
| Sound designers work in their familiar tool (Wwise Authoring) | **No** — audio team's familiar tool is their DAW, not Wwise |

Native Unity, by contrast, matches the team's actual workflow exactly:

- Audio team works in a DAW → exports `mp3` / `wav` → drops in `./sounds/`.
- Code team imports to `Assets/Sounds/` → wires up in a ScriptableObject → done.

There is no abstraction the team would use that Native Unity does not already provide. Wwise would add cost without solving any problem the team actually has.

**Note on the earlier solo-developer report**: an earlier version of this document, written before the team composition was known, rejected Wwise on solo-developer grounds. With the 4-person team, those specific arguments no longer applied — but the conclusion held under different reasoning, because the audio team chose a workflow that does not need middleware.

## 5. What This Means for the Team

### Audio team's responsibilities
- Source, record, edit, and master audio files in their DAW.
- Deliver files to `./sounds/` (repo root, source of truth).
- Decide musical content: chord choices, voicings, timbre/instrument.
- Iterate on audio by replacing files; the code team does not need to be involved per-tweak.

### Code team's responsibilities
- Maintain the sonification pipeline in `Assets/Sonification/`.
- Wire `./sounds/` files into the Unity project via a `ChordPalette` ScriptableObject (one-time per new file).
- Implement and tune the Aggregator → Mapper → Player pipeline (see `ARCHITECTURE.md`).
- Expose tunable parameters (loudness threshold, smoothing) in the Inspector so the audio team can A/B test without touching code.

### Inter-team handoff
- A shared list of "file name → (chord, note index)" mappings. Suggested format: a row in a simple spreadsheet or a markdown table inside `ARCHITECTURE.md`.
- File-naming convention: `{Chord}_{NoteIndex}.mp3` (e.g., `C_1.mp3`, `F_2.mp3`). Already followed by the current asset set.

### Audio iteration loop
1. Audio team re-records a clip and drops it in `./sounds/`.
2. Either team copies the file to `Assets/Sounds/` so Unity sees it.
3. If the file replaces an existing one with the same name, Unity reimports automatically. No code change needed.
4. Press Play, listen, repeat.

This loop is fast for content changes. It is intentionally slow for structural changes (adding new voices, restructuring the chord set), which is the right trade-off given the team's workflow choice.

## 6. Trade-offs We Are Accepting

- **Audio team's role is bounded at file delivery**: this is a deliberate choice, not a limitation imposed by the stack. Revisit only if the team's role definition itself changes.
- **Manual asset wiring**: adding a new chord means dropping files and assigning them in the Inspector. Acceptable at this scale (9 files now, plausibly under 30 ever).
- **No adaptive composition layer**: the sonification reflects the simulation literally. Intentional — the simulation *is* the composition.
- **Per-tick re-triggering, not looping**: each Player voice is re-triggered every N Game-of-Life ticks via `AudioSource.PlayOneShot`, with the trigger volume reflecting that tick's cell count. This gives the project its intended "the simulation is playing music" feel — each generation is a musical event, not a continuous backdrop. As a useful side effect, this approach bypasses the MP3 gapless-loop problem entirely (we never loop the clips), so the audio team can keep delivering mp3 without needing to switch to wav/ogg. If a more ambient, sustained-drone texture is wanted later, switch to `loop = true` with continuous volume modulation in the Player stage only.
- **No resume credit for "we used middleware"**: choosing the simpler stack means we don't get the Wwise line on a CV. The team accepted this trade-off in favor of focused, manageable scope.

## 7. When (If Ever) to Revisit

Reopen this decision only if one of the following becomes true:

- The audio team changes their stance and wants to own in-engine audio behavior directly.
- The number of distinct voices grows past ~30, where ScriptableObject wiring becomes tedious.
- A future feature genuinely requires middleware capabilities (sample variation, complex mixing, spatial audio, adaptive transitions).

If any of these happens, the 3-stage pipeline (Aggregator → Mapper → Player) in `ARCHITECTURE.md` is designed so that only the Player stage needs to change. Migration to Wwise — or to MPTK — does not require restructuring the rest of the code.

## 8. Summary

| Question | Answer |
|----------|--------|
| Chosen audio stack | **Native Unity (AudioSource + AudioMixer)** |
| Wwise considered? | Yes, seriously, given the 2+2 team structure. |
| Why not Wwise? | The audio team chose a DAW → file-delivery workflow. Wwise's central value (in-engine audio ownership) does not apply. |
| Is MPTK in contention? | No. Documented in `MPTK.md` as a future-iteration alternative. |
| Is the decision final? | Yes. Reopen only under the conditions in §7. |
| Where is the implementation plan? | `../plans/PLAN_2026_05_21.md` (v1 sprint, frozen). Architecture reference: `ARCHITECTURE.md`. |
