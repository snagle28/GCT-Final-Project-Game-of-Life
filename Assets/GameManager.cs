using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GameOfLifeManager : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Grid tilemapGrid;
    
    // Tile references for different cell states
    [Header("Sprites/ Images for Cell States")]
    [SerializeField] private TileBase noNeighborssTile;
    [SerializeField] private TileBase alive2NeighborsTile; // Green
    [SerializeField] private TileBase alive3NeighborsTile; // Blue
    [SerializeField] private TileBase alive4NeighborsTile; // New
    [SerializeField] private TileBase aliveOtherTile;      // Red
    
    // Grid settings
    [SerializeField] private int gridSize = 50;
    [SerializeField] private float baseUpdateInterval = 0.1f;
    
    // Game state
    private int[] cells;           // 0 = dead, 1 = alive
    private int[] age;             // Track cell age
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
        UpdateTilemap();
    }
    
    void NextGeneration()
    {
        int[] next = new int[cells.Length];
        System.Array.Copy(cells, next, cells.Length);
        
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
                        // Survives
                        age[p]++;
                    }
                }
                else
                {
                    // Cell is dead
                    if (neighbors == 3)
                    {
                        // Birth
                        next[p] = 1;
                        age[p] = 1;
                    }
                }
            }
        }
        
        cells = next;
        UpdateTilemap();
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
                    // Cell is alive - color by neighbor count
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
            }
        }
        UpdateTilemap();
    }
    
    void SetPattern(int[][] pattern, int offsetX, int offsetY)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            for (int j = 0; j < pattern[0].Length; j++)
            {
                if (pattern[i][j] == 1)
                {
                    ChangeCell(offsetX + j, offsetY + i, 1);
                    age[Pos(offsetX + j, offsetY + i)] = 10;
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
        if (Input.GetMouseButton(0)) // Mouse button held down
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(mouseWorldPos);
            
            if (gridPos.x >= 0 && gridPos.x < gridSize &&
                gridPos.y >= 0 && gridPos.y < gridSize)
            {
                ChangeCell(gridPos.x, gridPos.y, 1);
                UpdateCellTile(gridPos.x, gridPos.y);
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
                    ChangeCell(gridPos.x, gridPos.y, 1);
                    UpdateCellTile(gridPos.x, gridPos.y);
                }
            }
        }
    }

    void UpdateCellTile(int i, int j)
    {
        Vector3Int position = new Vector3Int(i, j, 0);
        int neighbors = CountAliveNeighbors(i, j);
        TileBase tile;

        if (neighbors == 2)
            tile = alive2NeighborsTile;
        else if (neighbors == 3)
            tile = alive3NeighborsTile;
        else if (neighbors == 4)
            tile = alive4NeighborsTile;
        else
            tile = aliveOtherTile;

        tilemap.SetTile(position, tile);
    }
    
    void SetPatternAtMouse(int[][] pattern)
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPos = tilemap.WorldToCell(mouseWorldPos);
        SetPattern(pattern, gridPos.x, gridPos.y);
    }
    
    // Getters for UI display
    public bool IsPaused => isPaused;
    public int SpeedLevel => speedLevel;
    public int AliveCount => CountAlive();
}