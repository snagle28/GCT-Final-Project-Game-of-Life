# MPTK / MIDI + Soundfont — Alternative Approach (For Future Consideration)

> **Status**: Not adopted for the current project. This document exists so the team is aware of the option and can consider it for later iterations or future projects.

## What It Is

**Maestro MPTK** (Midi Player Tool Kit) is a Unity package — installed from the Asset Store like any other library — that turns Unity into a real-time MIDI player and synthesizer. Instead of playing recorded audio files, you send musical events (note numbers, velocities, instrument changes) and a **soundfont** (`.sf2` file) synthesizes the audio on the fly.

Unlike Wwise, MPTK is just a library: no external authoring application, no SoundBank build step, no license server. You install the package and write C# — same deployment model as native Unity audio.

What makes it different from native Unity audio is the **level of abstraction**:

| Concept           | Native Unity Audio                  | MPTK (MIDI + Soundfont)                  |
|-------------------|-------------------------------------|------------------------------------------|
| Unit of playback  | `AudioClip` (a recorded audio file) | A MIDI note (pitch + velocity)           |
| Asset model       | One file per sound                  | One soundfont contains 128 pitches × many instruments |
| Pitch control     | Need a separate file per pitch      | `PlayNote(60)`, `PlayNote(67)`, etc.     |
| Loudness control  | `AudioSource.volume`                | MIDI velocity (also affects timbre)      |
| Instrument change | Re-record / re-source files         | Change soundfont patch number            |

In other words: native Unity is a **sample player**, MPTK is a **synthesizer-as-library**.

## Why It Would Be Interesting For This Project

The current sonification uses 9 fixed mp3 files. That is fine, but it locks the musical vocabulary to whatever was recorded. MPTK would unlock experiments such as:

- **Dynamic chord sets**: switch between C/F/G triads and Cm/Fm/Gm dim or jazz 7ths by editing a few integers — no new recordings.
- **Per-cell pitch**: instead of "one chord per color, one note per row," every single cell could be assigned its own MIDI note. The grid itself becomes a musical instrument layout.
- **Generative variation**: the simulation's evolving patterns could drive transposition, key changes, or scale shifts in real time.
- **Instrument morphing**: as the simulation grows or shrinks, the timbre could shift (piano → strings → bell) without any new audio assets.
- **Microtonal or non-Western tunings**: trivial with MIDI + a tuning table, painful with sample files.

For a project where "what does the grid *mean* musically?" is an open creative question, MIDI-based playback gives the team room to keep exploring without the bottleneck of recording new audio every time.

## Why We Are Not Adopting It For v1

1. **The mp3 assets already exist.** We have C/F/G triad recordings ready in `./sounds/`. Switching to MIDI throws away that prepared work.
2. **The audio team's workflow is DAW-based.** Audio designers typically work in DAWs (Logic, Ableton, Pro Tools) and hand off audio files. MIDI synthesis via a soundfont is more of a programmer's tool — adopting it would shift creative control toward the code team, which inverts the team structure we have.
3. **Soundfont curation is a separate skill.** Finding or building a good `.sf2` file that sounds the way the audio team wants is itself a task. The team is not currently set up for it.
4. **Scope discipline.** v1 already has enough moving parts (color reduction, voice pool, aggregator, mapper). Adding MIDI on top would expand learning surface area for unclear gain at this stage.

## When To Revisit

Consider MPTK if any of the following becomes true in a later iteration:

- The team wants to experiment with many chord/scale variations without re-recording.
- The "grid as instrument" idea (per-cell pitch) becomes a desired feature.
- The audio team becomes interested in generative or algorithmic composition.
- The project pivots from "fixed harmonic palette" to "evolving musical system."

## How To Try It Quickly

1. Install **Maestro MPTK Free** from the Unity Asset Store.
2. Add a `MidiStreamPlayer` component to a GameObject.
3. Replace the `AudioSource.volume = v` line in our `VoicePool` with:
   ```csharp
   midiPlayer.MPTK_PlayEvent(new MPTKEvent {
       Command = MPTKCommand.NoteOn,
       Value = midiNoteNumber,
       Velocity = (int)(v * 127f),
       Duration = -1   // sustain until NoteOff
   });
   ```
4. Map each (chord, noteIndex) to a MIDI note number. Done.

The 3-stage pipeline (Aggregator → Mapper → Player) in `ARCHITECTURE.md` is intentionally agnostic to playback backend — only the Player stage would change. This makes a future MPTK migration low-risk.

## Summary

MPTK is a library, not middleware. It sits in the same deployment category as native Unity audio but operates at the level of musical events instead of audio files. For this project's current scope (3 fixed chords, files already recorded, audio team works in DAWs), it is not the right choice. For a future iteration that wants generative or experimental musical behavior, it is the first tool to consider.
