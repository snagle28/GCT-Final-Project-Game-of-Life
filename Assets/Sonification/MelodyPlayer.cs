using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// AD1 + AD2: per-generation sonification using the ORIGINAL (v1) HISTOGRAM mapping,
// adapted to the current auto-loading clip infrastructure (swap sounds just by
// editing the Resources/Notes/{Forest,Ocean} folder — no code changes needed).
//
// v1 mapping (count -> loudness histogram):
//   * Every alive cell is binned by its color (chord) and by COLUMN:
//        noteIndex = column i % (number of clips that color provides)
//   * The CELL COUNT in each (color, note) bin drives that note's LOUDNESS:
//        volume = count / loudnessSaturationCount      (clamped 0..1)
//     Bins quieter than minAudibleVolume are skipped, so several notes per color
//     can sound at once — a chord whose internal balance reflects where the cells
//     of that color sit across the grid's columns.
//
// Playback (v1 re-struck a one-shot every tick, which piled up into a wall of sound
// with the long sustained note clips used here). Instead we keep the discrete attack
// but BOUND it two ways so the background loop stays audible:
//   * A fixed VOICE POOL with oldest-voice stealing caps total simultaneous notes.
//   * A per-bin RE-TRIGGER COOLDOWN stops a single note from re-stacking every tick.
//
// AD2 pan (kept from the later version — v1 had no panning): each color's notes are
// panned by that color's MEAN COLUMN, left(-1)..right(+1), widened by panStrength.
//
// Notes live in Resources/Notes/{Forest,Ocean} as "{Env}_Color{1..3}_{N}"
// (N = 1..however many; 1 = lowest). Add / remove / renumber files to remap the
// column -> note assignment; the count per color follows the files automatically.
// The set is chosen by the active scene (Soundscape2 = Ocean, else Forest).
public class MelodyPlayer : MonoBehaviour
{
    private const int ColorCount = 3;
    private const int PoolSize = 16;   // max simultaneous notes (loudness ceiling)

    // [color][step], low -> high. The COUNT per color is whatever clips that color
    // provides in Resources (colors may differ).
    private AudioClip[][] colorNotes;

    // Fixed pool of generic voices, reused via oldest-first stealing.
    private AudioSource[] voices;
    private float[] voiceStart;        // Time.time each voice last started (for stealing)

    private int tickCounter;
    private int maxNotes;              // widest note pool across colors (buffer width)
    private int[,] counts;             // [color, note] cell tally, reused each generation
    private float[,] lastPlay;         // [color, note] last trigger time (cooldown)

    // Scratch buffers reused each generation (no per-frame allocation).
    private readonly int[] count = new int[ColorCount];   // alive cells per color
    private readonly float[] sumX = new float[ColorCount]; // sum of columns per color
    private readonly float[] sumY = new float[ColorCount]; // 새로 추가: sum of rows (높이)

    void Awake()
    {
        LoadNotes();
        BuildVoices();
    }

    void LoadNotes()
    {
        // Soundscape2 = Ocean (env 2); everything else falls back to Forest (env 1).
        string env = SceneManager.GetActiveScene().name == "Soundscape2" ? "Ocean" : "Forest";

        // Gather every clip per color, keyed by its trailing index, then sort low->high.
        // Count per color = however many clips exist (the pool), no fixed number.
        var perColor = new List<KeyValuePair<int, AudioClip>>[ColorCount];
        for (int c = 0; c < ColorCount; c++) perColor[c] = new List<KeyValuePair<int, AudioClip>>();

        foreach (var clip in Resources.LoadAll<AudioClip>("Notes/" + env))
        {
            if (TryParseNoteName(clip.name, out int color, out int idx) &&
                color >= 0 && color < ColorCount)
                perColor[color].Add(new KeyValuePair<int, AudioClip>(idx, clip));
        }

        colorNotes = new AudioClip[ColorCount][];
        maxNotes = 0;
        for (int col = 0; col < ColorCount; col++)
        {
            perColor[col].Sort((a, b) => a.Key.CompareTo(b.Key)); // _1 = lowest .. _N = highest
            colorNotes[col] = new AudioClip[perColor[col].Count];
            for (int i = 0; i < perColor[col].Count; i++)
                colorNotes[col][i] = perColor[col][i].Value;

            maxNotes = Mathf.Max(maxNotes, colorNotes[col].Length);

            if (colorNotes[col].Length == 0)
                Debug.LogWarning($"MelodyPlayer: no note clips for color {col + 1} in " +
                                 $"Resources/Notes/{env} (expected '{env}_Color{col + 1}_N')");
        }

        int w = Mathf.Max(1, maxNotes);
        counts = new int[ColorCount, w];
        lastPlay = new float[ColorCount, w];
        for (int c = 0; c < ColorCount; c++)
            for (int n = 0; n < w; n++) lastPlay[c, n] = -999f;
    }

    // Parse a clip name like "{Env}_Color{C}_{N}" into color (0-based) and index N.
    static bool TryParseNoteName(string name, out int color, out int idx)
    {
        color = -1; idx = -1;
        string[] parts = name.Split('_');
        foreach (string p in parts)
            if (p.StartsWith("Color") && int.TryParse(p.Substring(5), out int c)) color = c - 1;
        if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int n)) idx = n;
        return color >= 0 && idx >= 0;
    }

    void BuildVoices()
    {
        voices = new AudioSource[PoolSize];
        voiceStart = new float[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject($"MelodyVoice_{i}");
            go.transform.SetParent(transform, worldPositionStays: false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f; // 2D — pan handled manually via panStereo
            voices[i] = src;
            voiceStart[i] = -999f;
        }
    }

    // Called once per generation by GameOfLifeManager.
    //   volume                  master note loudness (slider).
    //   panStrength             AD2 stereo spread applied to each color's mean column.
    //   triggerEveryNTicks      v1 tempo: only re-evaluate every N generations.
    //   loudnessSaturationCount cell count in a bin that maps to full volume (1.0).
    //   minAudibleVolume        bins quieter than this are skipped (not triggered).
    //   retriggerCooldown       seconds a given note must wait before re-striking
    //                           (stops one note from stacking into a wall).
    public void PlayGeneration(GameOfLifeManager game, float volume, float panStrength,
                               int triggerEveryNTicks, int loudnessSaturationCount,
                               float minAudibleVolume, float retriggerCooldown)
    {
        if (colorNotes == null || voices == null || counts == null) return;

        // v1 tempo: only re-evaluate every N generations.
        int everyN = Mathf.Max(1, triggerEveryNTicks);
        tickCounter++;
        if (tickCounter % everyN != 0) return;

        int size = game.GridSize;
        for (int c = 0; c < ColorCount; c++)
        {
            count[c] = 0; sumX[c] = 0f; sumY[c] = 0f;
            for (int n = 0; n < maxNotes; n++) counts[c, n] = 0;
        }

        // Histogram: bin every alive cell by color and by column (i % noteCount).
        for (int j = 0; j < size; j++)       // j = row
        {
            for (int i = 0; i < size; i++)   // i = column
            {
                int col = game.GetChordIndex(i, j);
                if (col < 0 || col >= ColorCount) continue; // dead cell

                count[col]++;
                sumY[col] += j;
                sumX[col] += i;
            }
        }

        float now = Time.time;
        float denom = size > 1 ? size - 1f : 1f;
        float master = Mathf.Clamp01(volume);
        int sat = Mathf.Max(1, loudnessSaturationCount);
        float cooldown = Mathf.Max(0f, retriggerCooldown);

        for (int col = 0; col < ColorCount; col++)
        {
            if (count[col] == 0) continue;

            // AD2 pan: this color's mean column, linear left(-1)..right(+1), widened.
            float meanX = sumX[col] / count[col];
            float pan = Mathf.Clamp(((meanX / denom) * 2f - 1f) * panStrength, -1f, 1f);

            int noteCount = colorNotes[col].Length;
            if (noteCount == 0) continue;

            // Y축(높이) 평균을 구해서 1개의 음(noteIdx)만 선택!
            float meanY = sumY[col] / count[col];
            float ratioY = meanY / denom;
            int noteIdx = Mathf.FloorToInt(ratioY * noteCount);
            noteIdx = Mathf.Clamp(noteIdx, 0, noteCount - 1);

            // 세포 개수에 따른 볼륨 
            float ratio = Mathf.Clamp01((float)count[col] / sat);
            if (ratio < minAudibleVolume) continue;
            
            // 쿨다운 체크
            if (now - lastPlay[col, noteIdx] < cooldown) continue;

            AudioClip clip = colorNotes[col][noteIdx];
            if (clip == null) continue;

            // 선택된 딱 1개의 음만 재생
            int v = AcquireVoice(now);                  
            voices[v].clip = clip;
            voices[v].panStereo = pan; // meanX로 계산된 pan 그대로 사용
            voices[v].volume = ratio * master;
            voices[v].Play();
            
            voiceStart[v] = now;
            lastPlay[col, noteIdx] = now;
        }
    }

    // Return a free voice index if any; otherwise steal the oldest-started one.
    int AcquireVoice(float now)
    {
        int oldest = 0;
        float oldestStart = float.MaxValue;
        for (int i = 0; i < voices.Length; i++)
        {
            if (!voices[i].isPlaying) return i;
            if (voiceStart[i] < oldestStart) { oldestStart = voiceStart[i]; oldest = i; }
        }
        voices[oldest].Stop();
        return oldest;
    }
}
