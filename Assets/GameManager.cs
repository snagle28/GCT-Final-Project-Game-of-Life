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
    [SerializeField] private int gridSize = 50;
    [SerializeField] private float baseUpdateInterval = 0.1f;

    // Sonification hook — optional; if unassigned the simulation runs silent
    [Header("Sonification")]
    [SerializeField] private Sonifier sonifier;

    // Paint color picker — optional; if unassigned, drawing falls back to
    // coloring cells by neighbor count (the original behavior).
    [Header("Drawing")]
    [SerializeField] private ColorPalette palette;

    // Game state
    private int[] cells;           // 0 = dead, 1 = alive
    private int[] age;             // Track cell age
    private int[] colorIndex;      // Per-cell stored color/chord: 0=Yellow/C, 1=Beige/F, 2=Blue/G
    private bool isPaused = true;
    private float timeSinceLastUpdate = 0f;
    private float currentUpdateInterval;

    // Speed control (like frame rate in Processing)
    private int speedLevel = 30;   // 1-60, higher = slower

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

    void Start()
    {
        InitializeGrid();
        //RandomSeedGrid(0.5f); // 50% chance like Processing
        currentUpdateInterval = baseUpdateInterval * speedLevel / 30f;
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
    }

    void InitializeGrid()
    {
        cells = new int[gridSize * gridSize];
        age = new int[gridSize * gridSize];
        colorIndex = new int[gridSize * gridSize];
    }

    void RandomSeedGrid(float aliveChance)
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                cells[Pos(i, j)] = Random.value < aliveChance ? 1 : 0;
                age[Pos(i, j)] = cells[Pos(i, j)];
            }
        }
        // Give the random seed colors based on neighbor count (one-time).
        for (int i = 0; i < gridSize; i++)
            for (int j = 0; j < gridSize; j++)
                if (cells[Pos(i, j)] == 1)
                    colorIndex[Pos(i, j)] = NeighborColorIndex(i, j);
        UpdateTilemap();
    }

    void NextGeneration()
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

        if (sonifier != null) sonifier.OnTick();
    }

    int Pos(int i, int j)
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

    void UpdateTilemap()
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
        }

        // W to speed up (lower speedLevel)
        if (Input.GetKeyDown(KeyCode.W))
        {
            speedLevel--;
            speedLevel = Mathf.Clamp(speedLevel, 1, 60);
            currentUpdateInterval = baseUpdateInterval * speedLevel / 30f;
            Debug.Log("Speed level: " + speedLevel);
        }

        // S to slow down (higher speedLevel)
        if (Input.GetKeyDown(KeyCode.S))
        {
            speedLevel++;
            speedLevel = Mathf.Clamp(speedLevel, 1, 60);
            currentUpdateInterval = baseUpdateInterval * speedLevel / 30f;
            Debug.Log("Speed level: " + speedLevel);
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
            isPaused = true;
            Debug.Log("P101 placed - paused");
        }
    }

    void HandleMouseInput()
    {
        // Don't paint the grid when the click lands on a palette button.
        if (palette != null && palette.IsPointerOverPalette()) return;

        if (Input.GetMouseButton(0)) // Mouse button held down
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(mouseWorldPos);

            if (gridPos.x >= 0 && gridPos.x < gridSize &&
                gridPos.y >= 0 && gridPos.y < gridSize)
            {
                PaintCell(gridPos.x, gridPos.y);
            }
        }

        if (Input.GetMouseButtonDown(0)) // Mouse button just clicked
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(mouseWorldPos);

            if (gridPos.x >= 0 && gridPos.x < gridSize &&
                gridPos.y >= 0 && gridPos.y < gridSize)
            {
                int index = Pos(gridPos.x, gridPos.y);
                if (cells[index] == 1)
                {
                    ChangeCell(gridPos.x, gridPos.y, 0);
                    tilemap.SetTile(gridPos, noNeighborssTile);
                }
                else
                {
                    PaintCell(gridPos.x, gridPos.y);
                }
            }
        }
    }

    // Sets a cell alive and stores the selected paint color so it persists through
    // the simulation and drives that cell's chord (see GetChordIndex).
    void PaintCell(int i, int j)
    {
        ChangeCell(i, j, 1);
        int p = Pos(i, j);
        colorIndex[p] = SelectedColorIndex();
        tilemap.SetTile(new Vector3Int(i, j, 0), TileForColor(colorIndex[p]));
    }

    void UpdateCellTile(int i, int j)
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
    TileBase TileForColor(int c)
    {
        if (c == 0) return alive2NeighborsTile;  // Yellow / C
        if (c == 1) return alive3NeighborsTile;  // Beige  / F
        return aliveOtherTile;                    // Blue   / G
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
    public int SpeedLevel => speedLevel;
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
}
