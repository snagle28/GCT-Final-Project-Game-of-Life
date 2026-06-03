using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// AD1 + AD2: per-generation melodic tones.
//
// Once per generation, every color that has at least one alive cell plays EXACTLY
// one note (a color with zero cells stays silent):
//   AD1 pitch <- mean Y of that color's cells, quantized into the color's own
//                12-note range, low at the bottom, high at the top.
//   AD2 pan   <- mean X of that color's cells, linear left(-1)..right(+1).
//
// Clips are environment-specific and live in Resources/Notes/{Forest,Ocean},
// named "{Env}_Color{1..3}_{1..12}" (step 1 = lowest, 12 = highest). The set is
// chosen by the active scene (Soundscape2 = Ocean, else Forest). Auto-loaded;
// the component is created on demand by GameOfLifeManager.
public class MelodyPlayer : MonoBehaviour
{
    private const int NotesPerColor = 12;
    private const int ColorCount = 3;
    private const int VoiceCount = 12;         // round-robin pool for overlap

    private AudioClip[][] colorNotes;          // [color][step 0..11], low -> high
    private AudioSource[] voices;
    private int nextVoice;
    private float lastTriggerTime = -999f;     // for tempo-decoupling throttle

    // Scratch buffers reused each generation (no per-frame allocation).
    private readonly int[] count = new int[ColorCount];
    private readonly float[] sumX = new float[ColorCount];
    private readonly float[] sumY = new float[ColorCount];

    void Awake()
    {
        LoadNotes();
        BuildVoices();
    }

    void LoadNotes()
    {
        // Soundscape2 = Ocean (env 2); everything else falls back to Forest (env 1).
        string env = SceneManager.GetActiveScene().name == "Soundscape2" ? "Ocean" : "Forest";

        var byName = new Dictionary<string, AudioClip>();
        foreach (var c in Resources.LoadAll<AudioClip>("Notes/" + env))
            byName[c.name] = c;

        colorNotes = new AudioClip[ColorCount][];
        for (int col = 0; col < ColorCount; col++)
        {
            colorNotes[col] = new AudioClip[NotesPerColor];
            for (int step = 0; step < NotesPerColor; step++)
            {
                // Files: {Env}_Color{1..3}_{1..12}.mp3 (step 0 -> "_1" = lowest).
                string nm = $"{env}_Color{col + 1}_{step + 1}";
                byName.TryGetValue(nm, out colorNotes[col][step]);
                if (colorNotes[col][step] == null)
                    Debug.LogWarning($"MelodyPlayer: missing note clip '{nm}' in Resources/Notes/{env}");
            }
        }
    }

    void BuildVoices()
    {
        voices = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            var go = new GameObject($"MelodyVoice_{i}");
            go.transform.SetParent(transform, worldPositionStays: false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            voices[i] = src;
        }
    }

    // Called once per generation by GameOfLifeManager.
    // panStrength > 1 pushes the stereo image harder toward the L/R extremes.
    // minInterval decouples the melody pulse from the sim tempo: at high speed the
    // melody won't retrigger faster than this (prevents note pile-up / dropouts).
    public void PlayGeneration(GameOfLifeManager game, float volume, float panStrength, float minInterval)
    {
        if (colorNotes == null || voices == null) return;

        // Throttle: skip this generation's melody if it would fire too soon.
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

        for (int col = 0; col < ColorCount; col++)
        {
            if (count[col] == 0) continue;   // no cells of this color -> no note
            float meanX = sumX[col] / count[col];
            float meanY = sumY[col] / count[col];

            // AD1: bottom -> low note (step 0), top -> high note (step 11).
            // Unity tilemap Y grows upward, so a high meanY is the top of the grid.
            float normY = Mathf.Clamp01(meanY / denom);
            int step = Mathf.RoundToInt(normY * (NotesPerColor - 1));

            // AD2: left -> -1, right -> +1. panStrength widens the image; mean X
            // tends toward center, so a >1 strength is needed for an extreme spread.
            float pan = ((meanX / denom) * 2f - 1f) * panStrength;

            AudioClip clip = colorNotes[col][step];
            if (clip == null) continue;

            // Bounded polyphony: reuse a voice via Play() (not PlayOneShot) so a
            // recycled voice cuts its previous note. Concurrent notes can never
            // exceed VoiceCount, so fast tempo can't pile up and steal voices.
            AudioSource src = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            src.clip = clip;
            src.panStereo = Mathf.Clamp(pan, -1f, 1f);
            src.volume = Mathf.Clamp01(volume);
            src.Play();
        }
    }
}
