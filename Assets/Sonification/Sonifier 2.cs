using UnityEngine;

// Glue stage: called once per Game-of-Life tick by GameOfLifeManager.
// Drives the pipeline:  Aggregator -> Mapper (inline here) -> VoicePool.
public class Sonifier : MonoBehaviour
{
    [SerializeField] private GameOfLifeManager game;
    [SerializeField] private ChordPalette palette;
    [SerializeField] private SonificationConfig config;
    [SerializeField] private VoicePool voicePool;

    private int tickCounter;

    public void OnTick()
    {
        if (palette == null || config == null || voicePool == null || game == null) return;

        tickCounter++;
        if (tickCounter % config.triggerEveryNTicks != 0) return;

        var counts = GridAggregator.Aggregate(game, palette);

        int chordCount = palette.ChordCount;
        for (int c = 0; c < chordCount; c++)
        {
            int notes = palette.GetNoteCount(c);
            for (int n = 0; n < notes; n++)
            {
                int count = counts[c, n];
                float volume = Mathf.Clamp01((float)count / config.loudnessSaturationCount);
                if (volume < config.minimumAudibleVolume) continue;
                voicePool.Trigger(c, n, volume);
            }
        }
    }
}
