using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// AD1 + AD2: per-generation melodic tones, played LEGATO (option 3 — smooth).
//
// The big idea: a note's sharp attack is what makes the melody feel "discrete", so
// we DON'T re-strike a color's note every generation. Instead each color behaves
// like one finger held on a key:
//   * Re-articulate only when that color's quantized pitch actually CHANGES (or when
//     its note has fully decayed and needs refreshing). On a pitch change the old
//     note crossfades into the new one (legato), so there is no gap or hard attack.
//   * While the pitch is unchanged and still ringing, the note simply sustains; we
//     only glide its stereo pan to follow the cells' mean X — movement without a
//     re-attack.
//   * A soft attack fade-in further rounds off each new note's onset.
//
//   AD1 pitch <- mean Y, quantized across however many notes that color provides
//                (bottom=low, top=high; level count = the color's clip pool, capped
//                at the grid's row count).
//   AD2 pan   <- mean X, linear left(-1)..right(+1), widened by panStrength.
//
// Live controls (all on GameOfLifeManager, applied per call so sliders respond in
// Play):
//   sustainSeconds    pedal hold — how long a held note rings before it fades out.
//   crossfadeSeconds  legato length when a pitch change replaces the old note.
//   attackSeconds     fade-in that softens each new note's onset.
//   maxVoicesPerColor safety cap on overlap (rarely hit now that we only re-articulate
//                     on change); oldest is crossfaded out when exceeded.
//
// Notes (~4s one-shots with their decay baked in) live in Resources/Notes/{Forest,
// Ocean} as "{Env}_Color{1..3}_{N}" (N = 1..however many; 1 = lowest). To remap the
// Y -> sound assignment just add / remove / renumber files in that folder — the count
// per color follows the files automatically. The set is chosen by the active scene
// (Soundscape2 = Ocean, else Forest). Auto-created on demand by GameOfLifeManager.
public class MelodyPlayer : MonoBehaviour
{
    private const int ColorCount = 3;
    private const int PoolSize = 32;          // hard ceiling near Unity's voice limit
    private const float PanGlide = 8f;        // how fast a held note's pan tracks meanX

    // [color][step], low -> high. The COUNT per color is whatever clips that color
    // provides in Resources (the pool size); colors may differ. Quantization uses
    // each color's own length, capped at the grid's row count.
    private AudioClip[][] colorNotes;

    // One ringing (or fading) note.
    private class Voice
    {
        public AudioSource src;
        public int color = -1;                 // -1 = free/idle
        public float startTime;
        public float baseVolume;
        public bool fading;
        public float fadeStartTime;
        public float fadeDuration;
        public float fadeFromVolume;           // volume captured when the fade began
    }

    private Voice[] voices;
    private readonly int[] lastStep = new int[ColorCount];   // -1 = color silent
    private float lastTriggerTime = -999f;                   // tempo-decoupling throttle

    // Latest tuning cached from UpdateVoices (used by the per-frame envelope work).
    private float curSustain = 2.5f;
    private float curAttack = 0.08f;
    private float curRelease = 0.25f;

    // Scratch buffers reused each generation (no per-frame allocation).
    private readonly int[] count = new int[ColorCount];
    private readonly float[] sumX = new float[ColorCount];
    private readonly float[] sumY = new float[ColorCount];

    void Awake()
    {
        for (int c = 0; c < ColorCount; c++) lastStep[c] = -1;
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
        for (int col = 0; col < ColorCount; col++)
        {
            perColor[col].Sort((a, b) => a.Key.CompareTo(b.Key)); // _1 = lowest .. _N = highest
            colorNotes[col] = new AudioClip[perColor[col].Count];
            for (int i = 0; i < perColor[col].Count; i++)
                colorNotes[col][i] = perColor[col][i].Value;

            if (colorNotes[col].Length == 0)
                Debug.LogWarning($"MelodyPlayer: no note clips for color {col + 1} in " +
                                 $"Resources/Notes/{env} (expected '{env}_Color{col + 1}_N')");
        }
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
        voices = new Voice[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject($"MelodyVoice_{i}");
            go.transform.SetParent(transform, worldPositionStays: false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            voices[i] = new Voice { src = src };
        }
    }

    // Called once per generation by GameOfLifeManager.
    public void PlayGeneration(GameOfLifeManager game, float volume, float panStrength,
                               float minInterval, int maxVoicesPerColor, float crossfadeSeconds)
    {
        if (colorNotes == null || voices == null) return;

        // Throttle: don't re-evaluate the melody faster than this (decouples from tempo).
        if (Time.time - lastTriggerTime < minInterval) return;
        lastTriggerTime = Time.time;

        int size = game.GridSize;
        for (int c = 0; c < ColorCount; c++) { count[c] = 0; sumX[c] = 0f; sumY[c] = 0f; }

        for (int j = 0; j < size; j++)       // j = row (0 = bottom, size-1 = top)
        {
            for (int i = 0; i < size; i++)   // i = column (0 = left, size-1 = right)
            {
                int col = game.GetChordIndex(i, j);
                if (col < 0 || col >= ColorCount) continue; // dead cell
                count[col]++;
                sumX[col] += i;
                sumY[col] += j;
            }
        }

        float denom = size > 1 ? size - 1f : 1f;
        float vol = Mathf.Clamp01(volume);
        int cap = Mathf.Max(1, maxVoicesPerColor);
        float xfade = Mathf.Max(0.01f, crossfadeSeconds);

        for (int col = 0; col < ColorCount; col++)
        {
            if (count[col] == 0)
            {
                // Color vanished -> let its note fade away and forget its pitch.
                if (lastStep[col] != -1) { FadeColor(col, xfade); lastStep[col] = -1; }
                continue;
            }

            AudioClip[] notes = colorNotes[col];
            int poolCount = notes != null ? notes.Length : 0;
            if (poolCount == 0) continue;            // this color has no clips

            float meanX = sumX[col] / count[col];
            float meanY = sumY[col] / count[col];

            // Pitch levels = this color's pool count, but never more than grid rows.
            int levels = Mathf.Min(poolCount, size);
            float normY = Mathf.Clamp01(meanY / denom);
            int step = levels > 1 ? Mathf.RoundToInt(normY * (levels - 1)) : 0;
            float pan = Mathf.Clamp(((meanX / denom) * 2f - 1f) * panStrength, -1f, 1f);

            AudioClip clip = notes[step];
            if (clip == null) continue;

            bool hasActive = HasActiveVoice(col);
            bool changed = step != lastStep[col];

            if (changed)
            {
                if (hasActive) FadeColor(col, xfade);     // legato: glide off the old pitch
                TriggerNote(col, clip, pan, vol, cap, xfade);
                lastStep[col] = step;
            }
            else if (!hasActive)
            {
                TriggerNote(col, clip, pan, vol, cap, xfade); // refresh a decayed note
                lastStep[col] = step;
            }
            else
            {
                SetColorPan(col, pan);                    // sustain: just track stereo
            }
        }
    }

    // Layer a new note for a color, crossfading out the oldest if the cap is reached.
    void TriggerNote(int color, AudioClip clip, float pan, float volume, int cap, float xfade)
    {
        EnforceCap(color, cap, xfade);

        Voice v = GetFreeVoice();
        if (v == null) return;   // pool exhausted (very rare) -> drop this note

        v.color = color;
        v.startTime = Time.time;
        v.baseVolume = volume;
        v.fading = false;
        v.src.clip = clip;
        v.src.panStereo = pan;
        v.src.volume = curAttack > 0f ? 0f : volume;   // fade-in handled in UpdateVoices
        v.src.Play();
    }

    // Per-frame envelope work, driven by GameOfLifeManager.Update (live tuning).
    public void UpdateVoices(float sustainSeconds, float attackSeconds, float releaseSeconds)
    {
        curSustain = Mathf.Max(0.05f, sustainSeconds);
        curAttack = Mathf.Max(0f, attackSeconds);
        curRelease = Mathf.Max(0.005f, releaseSeconds);
        if (voices == null) return;

        float now = Time.time;
        for (int i = 0; i < voices.Length; i++)
        {
            Voice v = voices[i];
            if (v.color < 0) continue;  // free

            if (!v.fading)
            {
                if (!v.src.isPlaying) { FreeVoice(v); continue; }   // sample ended on its own

                float age = now - v.startTime;
                if (age >= curSustain) { BeginFade(v, curRelease); }      // pedal released
                else if (curAttack > 0f && age < curAttack)               // soft onset
                    v.src.volume = Mathf.Lerp(0f, v.baseVolume, age / curAttack);
                else
                    v.src.volume = v.baseVolume;
            }

            if (v.fading)
            {
                float t = v.fadeDuration > 0f ? (now - v.fadeStartTime) / v.fadeDuration : 1f;
                if (t >= 1f) { v.src.Stop(); FreeVoice(v); }
                else v.src.volume = Mathf.Lerp(v.fadeFromVolume, 0f, t);
            }
        }
    }

    // If a color already has `cap` non-fading voices, crossfade out its oldest.
    void EnforceCap(int color, int cap, float xfade)
    {
        int active = 0;
        Voice oldest = null;
        for (int i = 0; i < voices.Length; i++)
        {
            Voice v = voices[i];
            if (v.color == color && !v.fading)
            {
                active++;
                if (oldest == null || v.startTime < oldest.startTime) oldest = v;
            }
        }
        if (active >= cap && oldest != null) BeginFade(oldest, xfade);
    }

    bool HasActiveVoice(int color)
    {
        for (int i = 0; i < voices.Length; i++)
        {
            Voice v = voices[i];
            if (v.color == color && !v.fading && v.src.isPlaying) return true;
        }
        return false;
    }

    // Glide the pan of a color's held (non-fading) note(s) toward the new value.
    void SetColorPan(int color, float pan)
    {
        for (int i = 0; i < voices.Length; i++)
        {
            Voice v = voices[i];
            if (v.color == color && !v.fading)
                v.src.panStereo = Mathf.MoveTowards(v.src.panStereo, pan, PanGlide * Time.deltaTime);
        }
    }

    void FadeColor(int color, float dur)
    {
        for (int i = 0; i < voices.Length; i++)
        {
            Voice v = voices[i];
            if (v.color == color && !v.fading) BeginFade(v, dur);
        }
    }

    void BeginFade(Voice v, float dur)
    {
        v.fading = true;
        v.fadeStartTime = Time.time;
        v.fadeDuration = Mathf.Max(0.005f, dur);
        v.fadeFromVolume = v.src.volume;
    }

    void FreeVoice(Voice v)
    {
        v.src.Stop();
        v.color = -1;
        v.fading = false;
    }

    Voice GetFreeVoice()
    {
        for (int i = 0; i < voices.Length; i++)
            if (voices[i].color < 0) return voices[i];
        return null;
    }
}
