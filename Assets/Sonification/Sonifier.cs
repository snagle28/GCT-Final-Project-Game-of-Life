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

        var result = GridAggregator.Aggregate(game, palette);
        var counts = result.counts;
        var averageXPositions = result.averageXPositions;

        int chordCount = palette.ChordCount;
        for (int c = 0; c < chordCount; c++)
        {
            int notes = palette.GetNoteCount(c);
            for (int n = 0; n < notes; n++)
            {
                int count = counts[c, n];
                float volume = Mathf.Clamp01((float)count / config.loudnessSaturationCount);
                if (volume < config.minimumAudibleVolume) continue;

                float pan = 0f;
                if (config.usePanning)
                {
                    pan = PositionToPanning(
                        averageXPositions[c, n],
                        0f,
                        game.GridSize - 1f,
                        config.panningSpread
                    );

                    pan = ExaggeratePan(pan);
                }

                voicePool.Trigger(c, n, volume, pan);
            }
        }
    }

    private static float PositionToPanning(float xPosition, float minX, float maxX, float panningSpread)
    {
        return Remap(xPosition, minX, maxX, -panningSpread, panningSpread);
    }

    private static float ExaggeratePan(float pan)
    {
        float absPan = Mathf.Abs(pan);

        if (absPan < 0.05f)
            return 0f;

        return Mathf.Sign(pan) * Mathf.Lerp(0.45f, 1f, absPan);
    }

    private static float Remap(float from, float fromMin, float fromMax, float toMin, float toMax)
    {
        if (Mathf.Approximately(fromMin, fromMax))
            return toMin;

        float normal = Mathf.InverseLerp(fromMin, fromMax, from);
        return Mathf.Lerp(toMin, toMax, normal);
    }
}
