using UnityEngine;

// Player stage of the sonification pipeline.
// Owns one AudioSource per (chord, note) slot. Trigger() fires PlayOneShot
// at the given volume; overlapping triggers naturally mix as decaying tails.
public class VoicePool : MonoBehaviour
{
    [SerializeField] private ChordPalette palette;

    private AudioSource[,] sources;

    void Awake()
    {
        if (palette == null)
        {
            Debug.LogError("VoicePool: ChordPalette is not assigned.");
            return;
        }
        BuildSources();
    }

    void BuildSources()
    {
        int chordCount = palette.ChordCount;
        int maxNotes = 0;
        for (int c = 0; c < chordCount; c++)
            maxNotes = Mathf.Max(maxNotes, palette.GetNoteCount(c));

        sources = new AudioSource[chordCount, maxNotes];

        for (int c = 0; c < chordCount; c++)
        {
            int notes = palette.GetNoteCount(c);
            for (int n = 0; n < notes; n++)
            {
                var go = new GameObject($"Voice_C{c}_N{n}");
                go.transform.SetParent(transform, worldPositionStays: false);
                var src = go.AddComponent<AudioSource>();
                src.clip = palette.GetClip(c, n);
                src.playOnAwake = false;
                src.loop = false;
                sources[c, n] = src;
            }
        }
    }

    public void Trigger(int chordIndex, int noteIndex, float volume)
    {
        if (sources == null) return;
        var src = sources[chordIndex, noteIndex];
        if (src == null || src.clip == null) return;
        src.PlayOneShot(src.clip, volume);
    }
}
