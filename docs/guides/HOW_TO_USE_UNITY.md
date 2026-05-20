# How to Use Unity (Setup Guide for First-Timers)

> If you have never opened Unity before and need to run the sonification feature in this repo, follow this guide top to bottom. Takes about 10 minutes.

A Korean version is available at `HOW_TO_USE_UNITY_KR.md`.

## Unity Editor Panels (Cheat Sheet)

- **Hierarchy** (top-left): list of GameObjects in the current scene.
- **Scene view** (center): visual editor for placing and inspecting GameObjects.
- **Game view** (center, tab): what the player sees when you press Play.
- **Inspector** (right): properties of the currently selected GameObject or asset.
- **Project** (bottom): file browser over the `Assets/` folder.
- **Console** (tab next to Project): error and log output. Always keep an eye on it.

## 0. Before You Start

1. Open this project in **Unity Hub** (Unity 6 / Unity 6000.x recommended; the project was built with Unity 6.4).
2. After the project opens, wait for the spinner in the bottom-right to stop. Unity is importing the mp3 files and compiling the new C# scripts.
3. Open **Window → General → Console** and dock it. If there are red errors, stop and fix those first — none of the steps below will work otherwise.

## 1. Open the Main Scene

In the **Project** panel (bottom), navigate to `Assets/Scenes/` and double-click the scene file (likely `Main Scene` or `SampleScene`).

You should now see:
- A grid in the **Scene view** (center).
- A GameObject named **`GameManager`** in the **Hierarchy** panel.

> **Naming note (important — read this once and you will not be confused later)**: the GameObject in the Hierarchy is named `GameManager`. The C# script attached to it is the `GameOfLifeManager` class. Unity slot fields type-match against the **script class**, not the GameObject name. So when a slot says `Game Of Life Manager` and you drag the `GameManager` GameObject onto it, Unity accepts it — because `GameManager` carries the `GameOfLifeManager` script. This is the convention everywhere in this guide.

## 2. Create the ChordPalette Asset

The `ChordPalette` is the editable mapping from chord → audio clips. The audio team will edit this file whenever they swap sounds.

1. In **Project**, open `Assets/Sonification/`.
2. Right-click in the empty area → **Create → Sonification → Chord Palette**.
3. Name the new asset `MainChordPalette`.

> If the **Sonification** menu does not appear, Unity is still compiling, or there is a compile error. Check the Console.

Click `MainChordPalette` to open it in the Inspector, then:

4. Set `Chords` → `Size` to **3**.
5. Fill the three entries (order matters — see warning below):

   | Index | Label     | Notes (drag from `Assets/Sounds/`)        |
   |-------|-----------|-------------------------------------------|
   | 0     | `C major` | `C_1.mp3`, `C_2.mp3`, `C_3.mp3`           |
   | 1     | `F major` | `F_1.mp3`, `F_2.mp3`, `F_3.mp3`           |
   | 2     | `G major` | `G_1.mp3`, `G_2.mp3`, `G_3.mp3`           |

> **Order is critical**: index 0 must be the chord assigned to 2-neighbor cells, 1 must be 3-neighbor cells, 2 must be everything else. This matches `GameOfLifeManager.GetChordIndex`. If you reorder, the wrong chord will play for each visual color.

## 3. Create the SonificationConfig Asset

This asset holds tunable numbers (trigger cadence, loudness saturation, minimum volume).

1. In **Project**, `Assets/Sonification/`, right-click → **Create → Sonification → Config**.
2. Name it `MainSonificationConfig`.

Defaults are fine for the first test:

- `Trigger Every N Ticks` = 2
- `Loudness Saturation Count` = 8
- `Minimum Audible Volume` = 0.02

## 4. Create the VoicePool GameObject

The `VoicePool` is the runtime audio playback engine — it builds one `AudioSource` per chord×note slot at startup.

1. In **Hierarchy**, right-click empty space → **Create Empty**.
2. Rename the new GameObject to **`VoicePool`** (press F2 or double-click the name).
3. With `VoicePool` selected, in the **Inspector**, click **Add Component** at the bottom.
4. Type `VoicePool` in the search box and click the result to add the component.
5. The new component appears with a `Palette` slot. Drag `MainChordPalette` from the **Project** panel into this slot.

## 5. Create the Sonifier GameObject

The `Sonifier` is the glue — it is called every Game-of-Life tick and drives the sonification pipeline.

1. In **Hierarchy**, right-click → **Create Empty**. Rename to **`Sonifier`**.
2. In the **Inspector**, **Add Component** → search `Sonifier` → add.
3. Fill the four reference slots (drag-and-drop, no typing):

   | Slot         | Drag this onto it                                                                                |
   |--------------|--------------------------------------------------------------------------------------------------|
   | `Game`       | Hierarchy → **`GameManager`** (the GameObject — Unity matches by script class, see §1 note)      |
   | `Palette`    | Project → **`MainChordPalette`**                                                                  |
   | `Config`     | Project → **`MainSonificationConfig`**                                                            |
   | `Voice Pool` | Hierarchy → **`VoicePool`**                                                                       |

When done, no slot should read "None (...)".

## 6. Wire the Sonifier into the GameManager

The simulation needs to know which Sonifier to call each tick.

1. In **Hierarchy**, click **`GameManager`**.
2. In the **Inspector**, find the `Game Of Life Manager` component and look under the **`Sonification`** header.
3. Drag the **`Sonifier`** GameObject (from Hierarchy) into the empty **`Sonifier`** slot.

## 7. Audio Output Check

Quick sanity checks before pressing Play:

- In **Hierarchy**, click **Main Camera**. In the Inspector, confirm it has an **Audio Listener** component. (Default Unity scenes have one. If it is missing, **Add Component → Audio Listener**.)
- At the top of the **Game view**, the speaker icon (**Mute Audio**) should be **off** (not highlighted).
- Your OS volume is up.

## 8. Play and Test

1. Click the **▶ Play** button at the top of the editor.
2. Press **Space** to unpause (the simulation starts paused by default).
3. Click in the grid with the mouse to draw some live cells, or use a shortcut:
   - `G` — place a glider at the cursor
   - `M` — clear and place the P101 pattern
4. As cells evolve, you should hear chord notes triggering every 2 ticks, with volume proportional to how many same-color cells are in each row.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| No sound at all | Game view Mute is off; OS volume is up; Main Camera has an Audio Listener. |
| Red errors in Console | Read the error. Usually a missing reference or unassigned slot. Stop and fix before continuing. |
| Visual updates but no audio | The `Sonifier` slot on `GameManager` (Step 6) is probably still empty. |
| `Sonification` menu missing in Create | Scripts have not compiled yet, or have errors. Check the Console. |
| Too loud or harsh | In `MainSonificationConfig`, raise `Loudness Saturation Count` (try 16 or 32). |
| Too dense, no rhythm | Raise `Trigger Every N Ticks` to 3 or 4, and/or press `S` in Play mode to slow down the simulation. |
| Too sparse, only quiet notes | Raise `Minimum Audible Volume` to 0.05 or 0.1. |
| Wrong chord plays for a color | The order of entries in `MainChordPalette` is wrong. Must be 0=C, 1=F, 2=G. |

## Where to Go Next

- `../reference/ARCHITECTURE.md` — system architecture (3-stage pipeline, team roles, modification guide).
- `../plans/PLAN_2026_05_21.md` — v1 sprint plan (the steps and decisions that built what you just set up).
- `../log/LOG.md` — chronological history of what has been built and why.
- `../reference/REPORT.md` — why this audio stack was chosen over alternatives like Wwise.
- `../reference/MPTK.md` — an alternative MIDI-based approach we considered but did not adopt.
