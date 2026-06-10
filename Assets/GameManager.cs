using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GameOfLifeManager : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Grid tilemapGrid;

    // Tile references for different cell states.
    // Color index convention (stored per cell): 0 = Yellow → C, 1 = Beige → F, 2 = Blue → G.
    [Header("Sprites/ Images for Cell States")]
    [SerializeField] private TileBase noNeighborssTile;
    [SerializeField] private TileBase alive2NeighborsTile; // Yellow → C chord (color 0)
    [SerializeField] private TileBase alive3NeighborsTile; // Beige  → F chord (color 1)
    [SerializeField] private TileBase aliveOtherTile;      // Blue   → G chord (color 2)
    //ㅅㄷㄴ셔ㅠㅎtesting github
    // Grid settings
    [SerializeField] public int gridSize = 50;
    [SerializeField] private float baseUpdateInterval = 0.1f;

    // AD1/AD2: per-generation melodic tones (color -> chord, mean Y -> pitch,
    // mean X -> stereo pan). Auto-created in Start; clips load from Resources/Notes.
    [Header("Sonification")]
    [SerializeField] private MelodyPlayer melody;
    [SerializeField, Range(0f, 1f)] private float melodyVolume = 0.6f; // AD1/AD2 note loudness
    [SerializeField, Range(1f, 4f)] private float melodyPanStrength = 2.5f; // AD2 stereo spread (higher = more extreme)
    [SerializeField, Range(0f, 0.5f)] private float melodyMinInterval = 0.12f; // min seconds between melody pulses (decouples from tempo)
    [SerializeField, Range(0.1f, 4f)] private float melodySustainSeconds = 3.5f; // "pedal" hold: how long a held note rings before fading out
    [SerializeField, Range(0.02f, 1f)] private float melodyCrossfadeSeconds = 0.3f; // legato glide when a color's pitch changes
    [SerializeField, Range(0f, 0.5f)] private float melodyAttackSeconds = 0.08f;  // soft fade-in that rounds off each new note's onset
    [SerializeField, Range(0.02f, 1f)] private float melodyReleaseSeconds = 0.3f; // fade-out when a held note's pedal time ends
    [SerializeField, Range(1, 10)] private int melodyMaxVoicesPerColor = 6;       // safety cap on overlapping notes per color

    // AD3: click effect sound played when painting a cell. Auto-created in Start
    // if left unassigned; loads its clips for the active soundscape from Resources.
    [SerializeField] private FootstepPlayer footstep;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 1f; // AD3 click-sound loudness

    // AD4: start/stop effect sound played on the play/pause toggle (space bar).
    // Auto-created in Start if left unassigned; loads its clip from Resources.
    [SerializeField] private StartSoundPlayer startSound;
    [SerializeField, Range(0f, 1f)] private float startSoundVolume = 1f; // AD4 start/stop-sound loudness

    // AD5: continuous background loop; volume driven by total alive-cell count.
    // Auto-created in Start if left unassigned; loads its clip from Resources.
    [SerializeField] private BackgroundLoopPlayer backgroundLoop;
    [SerializeField, Range(0f, 1f)] private float bgMinVolume = 0.1f;   // volume at 0 cells
    [SerializeField, Range(0f, 1f)] private float bgMaxVolume = 0.8f;   // volume once cells reach the cap
    [SerializeField, Range(0.01f, 1f)] private float bgCapFraction = 0.3f; // cap = this fraction of all cells
    [SerializeField, Range(0.01f, 1f)] private float bgSmoothing = 0.1f;   // per-frame volume lerp (anti-jump)

    // Paint color picker — optional; if unassigned, drawing falls back to
    // coloring cells by neighbor count (the original behavior).
    [Header("Drawing")]
    [SerializeField] private ColorPalette palette;

    // Game state
    [System.NonSerialized] public int[] cells;
    [System.NonSerialized] public int[] age;
    [System.NonSerialized] public int[] colorIndex;    // Per-cell stored color/chord: 0=Yellow/C, 1=Beige/F, 2=Blue/G
    private bool isPaused = true;
    private float timeSinceLastUpdate = 0f;
    private float currentUpdateInterval = 1f;
    private bool isBeginningCutscene;
    
    // Pattern definitions (matching Processing patterns)
private int[][] glider = {
        new int[] {0,1,0},
        new int[] {0,0,1},
        new int[] {1,1,1}
    };

    private int[][] blinker = {
        new int[] {1,1,1}
    };

    private int[][] toad = {
       new int[] {0,1,1,1},
       new int[] {1,1,1,0}
    };

    private int[][] p101 = {
        new int[] {0,1,1,0,0,0,1,1,0},
        new int[] {1,0,0,1,0,1,0,0,1},
        new int[] {1,0,0,1,0,1,0,0,1},
        new int[] {0,1,1,0,1,0,1,1,0},
        new int[] {0,0,0,0,0,0,0,0,0},
        new int[] {1,0,1,0,0,0,1,0,1},
        new int[] {1,0,1,0,0,0,1,0,1},
        new int[] {0,1,0,0,0,0,0,1,0}
    };
    
    private int[][] diamond = {
        new int[] {0,0,1,1,1,0,0},
        new int[] {0,1,1,1,1,1,0},
        new int[] {1,1,1,1,1,1,1},
        new int[] {0,1,1,1,1,1,0},
        new int[] {0,0,1,1,1,0,0}
    };

    private int[][] pulsar = {
        new int[] {0,0,1,1,1,0,0,0,1,1,1,0,0},
        new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0},
        new int[] {1,0,0,0,0,1,0,1,0,0,0,0,1},
        new int[] {1,0,0,0,0,1,0,1,0,0,0,0,1},
        new int[] {1,0,0,0,0,1,0,1,0,0,0,0,1},
        new int[] {0,0,1,1,1,0,0,0,1,1,1,0,0},
        new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0},
        new int[] {0,0,1,1,1,0,0,0,1,1,1,0,0},
        new int[] {1,0,0,0,0,1,0,1,0,0,0,0,1},
        new int[] {1,0,0,0,0,1,0,1,0,0,0,0,1},
        new int[] {1,0,0,0,0,1,0,1,0,0,0,0,1},
        new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0},
        new int[] {0,0,1,1,1,0,0,0,1,1,1,0,0}
    };

    void Start()
    {
        InitializeGrid();
        //RandomSeedGrid(0.5f); // 50% chance like Processing

        isBeginningCutscene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Beginning Cutscene";

        // AD1/AD2: ensure the per-generation melody player exists.
if (melody == null) melody = gameObject.AddComponent<MelodyPlayer>();

        // AD3: ensure a footstep click-sound player exists (volume passed per Play).
        if (footstep == null) footstep = gameObject.AddComponent<FootstepPlayer>();

        // AD4: ensure a start/stop-sound player exists (volume passed per Play).
        if (startSound == null) startSound = gameObject.AddComponent<StartSoundPlayer>();

        // AD5: ensure the background loop player exists (it starts playing on Awake).
        if (backgroundLoop == null) backgroundLoop = gameObject.AddComponent<BackgroundLoopPlayer>();
    }

    void Update()
    {
        // Handle keyboard input
        HandleInput();

        // Update simulation
        if (!isPaused)
        {
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate >= currentUpdateInterval)
            {
                NextGeneration();
                timeSinceLastUpdate = 0f;
            }
        }

        // Handle mouse drawing
        HandleMouseInput();

        // AD1/AD2: per-frame melody envelope work (attack fade-in, pedal release).
        if (melody != null && !isBeginningCutscene)
            melody.UpdateVoices(melodySustainSeconds, melodyAttackSeconds, melodyReleaseSeconds);

        // AD5: drive the background loop's volume from the total alive-cell count.
        if (backgroundLoop != null && !isBeginningCutscene)
        {
            int cap = Mathf.RoundToInt(gridSize * gridSize * bgCapFraction);
            backgroundLoop.UpdateLevel(CountAlive(), cap, bgMinVolume, bgMaxVolume, bgSmoothing);
        }
        else if (backgroundLoop != null && isBeginningCutscene)
        {
             // Keep it silent during the cutscene
            backgroundLoop.UpdateLevel(0, 1, 0, 0, 1);
        }
    }

    void InitializeGrid()
    {
        cells = new int[gridSize * gridSize];
        age = new int[gridSize * gridSize];
        colorIndex = new int[gridSize * gridSize];
    }
    

    public void NextGeneration()
    {
        int[] next = new int[cells.Length];
        System.Array.Copy(cells, next, cells.Length);

        // Copy colors forward: surviving cells keep their color; newborns get one below.
        int[] nextColor = new int[colorIndex.Length];
        System.Array.Copy(colorIndex, nextColor, colorIndex.Length);

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                int p = Pos(i, j);
                int neighbors = CountAliveNeighbors(i, j);

                if (cells[p] == 1)
                {
                    // Cell is alive
                    if (neighbors < 2 || neighbors > 3)
                    {
                        // Dies from underpopulation or overpopulation
                        next[p] = 0;
                        age[p] = 0;
                    }
                    else
                    {
                        // Survives — keeps its existing color (nextColor[p] already copied).
                        age[p]++;
                    }
                }
                else
                {
                    // Cell is dead
                    if (neighbors == 3)
                    {
                        // Birth — inherit the majority color of the 3 living parents.
                        next[p] = 1;
                        age[p] = 1;
                        nextColor[p] = InheritColorIndex(i, j);
                    }
                }
            }
        }

        cells = next;
        colorIndex = nextColor;
        UpdateTilemap();

        // AD1/AD2: one note per color this generation (mean Y -> pitch, mean X -> pan).
        if (melody != null && !isBeginningCutscene) melody.PlayGeneration(this, melodyVolume, melodyPanStrength, melodyMinInterval, melodyMaxVoicesPerColor, melodyCrossfadeSeconds);
    }

    public int Pos(int i, int j)
    {
        // Clamp to grid bounds (not wrapping, like Processing)
        i = Mathf.Clamp(i, 0, gridSize - 1);
        j = Mathf.Clamp(j, 0, gridSize - 1);
        return i + j * gridSize;
    }

    int CountAliveNeighbors(int i, int j)
    {
        int count = 0;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                count += cells[Pos(i + x, j + y)];
            }
        }
        return count;
    }

    public void UpdateTilemap()
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                Vector3Int position = new Vector3Int(i, j, 0);
                int index = Pos(i, j);

                if (cells[index] == 1)
                {
                    // Cell is alive - draw its stored color
                    UpdateCellTile(i, j);
                }
                else
                {
                    // Cell is dead
                    tilemap.SetTile(position, noNeighborssTile);
                }
            }
        }
    }

    void ChangeCell(int i, int j, int value)
    {
        cells[Pos(i, j)] = value;
        if (value == 0)
            age[Pos(i, j)] = 0;
    }

    int CountAlive()
    {
        int count = 0;
        foreach (int cell in cells)
        {
            count += cell;
        }
        return count;
    }

    void ClearGrid()
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                cells[Pos(i, j)] = 0;
                age[Pos(i, j)] = 0;
                colorIndex[Pos(i, j)] = 0;
            }
        }
        UpdateTilemap();
    }

    void SetPattern(int[][] pattern, int offsetX, int offsetY)
    {
        // Patterns are stamped with the currently selected paint color (or color 0).
        int ci = SelectedColorIndex();
        for (int i = 0; i < pattern.Length; i++)
        {
            for (int j = 0; j < pattern[0].Length; j++)
            {
                if (pattern[i][j] == 1)
                {
                    ChangeCell(offsetX + j, offsetY + i, 1);
                    age[Pos(offsetX + j, offsetY + i)] = 10;
                    colorIndex[Pos(offsetX + j, offsetY + i)] = ci;
                }
            }
        }
        UpdateTilemap();
    }

    void HandleInput()
    {
        // Spacebar to pause/unpause
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
            Debug.Log("Paused: " + isPaused);

            // AD4: play the start/stop effect on every play/pause toggle.
            if (startSound != null && !isBeginningCutscene) startSound.Play(startSoundVolume);
        }

        // C to clear
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearGrid();
            Debug.Log("Grid cleared");
        }

        // Pattern shortcuts
        if (Input.GetKeyDown(KeyCode.G))
        {
            SetPatternAtMouse(glider);
            Debug.Log("Glider placed");
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            SetPatternAtMouse(blinker);
            Debug.Log("Blinker placed");
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            SetPatternAtMouse(toad);
            Debug.Log("Toad placed");
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            ClearGrid();
            SetPatternAtMouse(p101);
            isPaused = true; //place moth
            Debug.Log("P101 placed - paused");
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            SetPatternAtMouse(diamond);
            Debug.Log("Diamond placed");
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            SetPatternAtMouse(pulsar);
            Debug.Log("Pulsar placed");
        }
    }

    void HandleMouseInput()
    {
        // Don't paint or erase when the click lands on a palette button.
        if (palette != null && palette.IsPointerOverPalette()) return;

        bool left = Input.GetMouseButton(0);
        bool right = Input.GetMouseButton(1);

        if (left || right)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(mouseWorldPos);

            if (gridPos.x >= 0 && gridPos.x < gridSize &&
                gridPos.y >= 0 && gridPos.y < gridSize)
            {
                if (left)
                {
                    PaintCell(gridPos.x, gridPos.y);
                }
                else if (right)
                {
                    EraseCell(gridPos.x, gridPos.y);
                }
            }
        }
    }

    void EraseCell(int i, int j)
    {
        int p = Pos(i, j);
        if (cells[p] == 1)
        {
            ChangeCell(i, j, 0);
            tilemap.SetTile(new Vector3Int(i, j, 0), noNeighborssTile);
        }
    }

    // Sets a cell alive and stores the selected paint color so it persists through
// the simulation and drives that cell's chord (see GetChordIndex).
    void PaintCell(int i, int j)
    {
        int p = Pos(i, j);
        bool wasDead = cells[p] == 0;
        ChangeCell(i, j, 1);
        colorIndex[p] = SelectedColorIndex();
        tilemap.SetTile(new Vector3Int(i, j, 0), TileForColor(colorIndex[p]));

        // AD3: play a random click effect only when a cell is newly placed, so
        // dragging across already-painted cells doesn't machine-gun the sound.
        if (wasDead && footstep != null && !isBeginningCutscene) footstep.Play(footstepVolume);
    }

    public void UpdateCellTile(int i, int j)
    {
        Vector3Int position = new Vector3Int(i, j, 0);
        tilemap.SetTile(position, TileForColor(colorIndex[Pos(i, j)]));
    }

    void SetPatternAtMouse(int[][] pattern)
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPos = tilemap.WorldToCell(mouseWorldPos);
        SetPattern(pattern, gridPos.x, gridPos.y);
    }

    // ---- Color helpers ----------------------------------------------------

    // The color index the user is currently painting with (palette selection),
    // falling back to color 0 when no palette/selection is available.
    int SelectedColorIndex()
    {
        if (palette != null && palette.SelectedTile != null)
            return ColorIndexForTile(palette.SelectedTile);
        return 0;
    }

    // Maps a stored color index to its tile (visual + chord color).
    public TileBase TileForColor(int c)
    {
        if (c == 0) { Debug.Log("Returning yellow tile: " + alive2NeighborsTile?.name); return alive2NeighborsTile; }
        if (c == 1) { Debug.Log("Returning beige tile: " + alive3NeighborsTile?.name); return alive3NeighborsTile; }
        Debug.Log("Returning blue tile: " + aliveOtherTile?.name);
        return aliveOtherTile;
    }

    // Maps a palette tile back to a color index. Unknown tiles default to 0.
    int ColorIndexForTile(TileBase t)
    {
        if (t == alive2NeighborsTile) return 0;
        if (t == alive3NeighborsTile) return 1;
        if (t == aliveOtherTile) return 2;
        return 0;
    }

    // Color index derived from neighbor count (the original scheme), used as a
    // fallback when a cell has no painted color (e.g. the random seed).
    int NeighborColorIndex(int i, int j)
    {
        int n = CountAliveNeighbors(i, j);
        if (n == 2) return 0;
        if (n == 3) return 1;
        return 2;
    }

    // Majority color of the living neighbors (a newborn has exactly 3 parents).
    // Ties resolve to the lowest color index.
    int InheritColorIndex(int i, int j)
    {
        int[] tally = new int[3];
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                int np = Pos(i + x, j + y);
                if (cells[np] == 1)
                {
                    int c = colorIndex[np];
                    if (c >= 0 && c < 3) tally[c]++;
                }
            }
        }
        int best = 0;
        for (int c = 1; c < 3; c++)
            if (tally[c] > tally[best]) best = c;
        return best;
    }

    // Getters for UI display
    public bool IsPaused => isPaused;
    public int AliveCount => CountAlive();

    // Sonification accessors. Chord index convention: 0 = C, 1 = F, 2 = G.
    // Now driven by the cell's stored color, so audio voice == painted color.
    public int GridSize => gridSize;

    public int GetChordIndex(int i, int j)
    {
        int p = Pos(i, j);
        if (cells[p] == 0) return -1;
        return colorIndex[p];
    }
    
    // Slider value: 0 = slowest (~1/sec), 1 = fastest (~30/sec)
    public void SetSpeedFromSlider(float t)
    {
        // Exponential curve: gives perceptually even spacing across the range
        // t=0 → interval=1.0s, t=1 → interval=0.033s (~30/sec)
        float minInterval = 1f / 30f;
        float maxInterval = 1f;
        currentUpdateInterval = Mathf.Lerp(maxInterval, minInterval, Mathf.Sqrt(t));
    }
}
