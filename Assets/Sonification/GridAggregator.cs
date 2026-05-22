using UnityEngine;

// Aggregator stage of the sonification pipeline.
// Walks every alive cell, classifies it by chord (via GameOfLifeManager),
// and increments counts[chordIndex, noteIndex] where noteIndex = row % chord.Length.
//
// Swapping the aggregation axis (e.g., row → column) is a localized edit
// inside this file: change `j` (row) to `i` (column) in the noteIndex computation.
//
// Also tracks the average X position of the cells contributing to each voice,
// so the Sonifier can pan that voice left/right.
public static class GridAggregator
{
    public struct AggregationResult
    {
        public int[,] counts;
        public float[,] averageXPositions;
    }

    public static AggregationResult Aggregate(GameOfLifeManager game, ChordPalette palette)
    {
        int chordCount = palette.ChordCount;
        int maxNotes = 0;
        for (int c = 0; c < chordCount; c++)
            maxNotes = Mathf.Max(maxNotes, palette.GetNoteCount(c));

        var counts = new int[chordCount, maxNotes];
        var xSums = new float[chordCount, maxNotes];
        var averageXPositions = new float[chordCount, maxNotes];

        int size = game.GridSize;

        for (int j = 0; j < size; j++)        // j = row
        {
            for (int i = 0; i < size; i++)    // i = column
            {
                int chord = game.GetChordIndex(i, j);
                if (chord < 0) continue;       // cell is dead

                int noteCount = palette.GetNoteCount(chord);
                int noteIdx = j % noteCount;

                counts[chord, noteIdx]++;
                xSums[chord, noteIdx] += i;
            }
        }

        for (int c = 0; c < chordCount; c++)
        {
            int notes = palette.GetNoteCount(c);
            for (int n = 0; n < notes; n++)
            {
                if (counts[c, n] > 0)
                    averageXPositions[c, n] = xSums[c, n] / counts[c, n];
            }
        }

        return new AggregationResult
        {
            counts = counts,
            averageXPositions = averageXPositions
        };
    }
}
