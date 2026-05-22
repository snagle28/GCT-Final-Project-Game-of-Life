using UnityEngine;

[CreateAssetMenu(fileName = "SonificationConfig", menuName = "Sonification/Config")]
public class SonificationConfig : ScriptableObject
{
    [Tooltip("Re-trigger voices every N Game-of-Life ticks. Higher = sparser, slower musical pulse.")]
    [Min(1)] public int triggerEveryNTicks = 2;

    [Tooltip("Cell count that maps to maximum loudness (1.0). Counts above this clip to 1.0.")]
    [Min(1)] public int loudnessSaturationCount = 8;

    [Tooltip("Voices whose computed volume falls below this are skipped (not triggered).")]
    [Range(0f, 1f)] public float minimumAudibleVolume = 0.02f;

    [Header("Stereo Panning")]
    [Tooltip("If enabled, notes will pan left/right based on the average X position of contributing cells.")]
    public bool usePanning = true;

    [Tooltip("How wide the stereo panning should be. 0 = centered, 1 = full left/right.")]
    [Range(0f, 1f)] public float panningSpread = 1f;
}
