using UnityEngine;

[CreateAssetMenu(fileName = "ChordPalette", menuName = "Sonification/Chord Palette")]
public class ChordPalette : ScriptableObject
{
    [System.Serializable]
    public class ChordEntry
    {
        public string label;          // For Inspector readability, e.g., "C major"
        public AudioClip[] notes;     // One clip per note of the chord
    }

    // Index in this array corresponds to the chord index returned by
    // GameOfLifeManager.GetChordIndex. Keep ordering stable: 0=C, 1=F, 2=G.
    public ChordEntry[] chords;

    public int ChordCount => chords?.Length ?? 0;

    public int GetNoteCount(int chordIndex) => chords[chordIndex].notes.Length;

    public AudioClip GetClip(int chordIndex, int noteIndex) =>
        chords[chordIndex].notes[noteIndex];
}
