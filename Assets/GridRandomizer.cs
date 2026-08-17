using UnityEngine;

// Fills the grid with a fresh random board: every cell is decided independently,
// so blanks and colors end up genuinely scattered rather than forming a pattern.
//
// Each cell is alive with probability `density`; a living cell then draws one of
// the `colorCount` paint colors uniformly at random (0 = Yellow/C, 1 = Beige/F,
// 2 = Blue/G, matching GameOfLifeManager's color convention).
//
// Randomness comes from a System.Random seeded once from a Guid, so neither two
// runs of the build nor two presses in the same run repeat a board.
//
// Setup: add this to the GameManager object, leave `game` empty to auto-find the
// manager, then wire a UI Button's OnClick to Randomize() and/or press
// `randomizeKey` (default R).
public class GridRandomizer : MonoBehaviour
{
    [Tooltip("Leave empty to find the GameOfLifeManager in the scene automatically.")]
    [SerializeField] private GameOfLifeManager game;

    [Tooltip("Chance that any single cell starts alive. 0 = empty board, 1 = full board.")]
    [SerializeField, Range(0f, 1f)] private float density = 0.3f;

    [Tooltip("How many paint colors a living cell can get. 3 = Yellow/Beige/Blue.")]
    [SerializeField, Range(1, 3)] private int colorCount = 3;

    [Tooltip("Pause the simulation after randomizing so the board can be inspected.")]
    [SerializeField] private bool pauseAfterRandomize = true;

    [Tooltip("Randomize once when the scene loads.")]
    [SerializeField] private bool randomizeOnStart = false;

    [Tooltip("Keyboard shortcut for Randomize(). Set to None to disable.")]
    [SerializeField] private KeyCode randomizeKey = KeyCode.R;

    // Seeded from a Guid rather than the frame count or startup time, so the
    // sequence doesn't depend on when in the session the first press happens.
    private System.Random rng;
    private bool pendingStartRandomize;

    void Awake()
    {
        if (game == null) game = Object.FindFirstObjectByType<GameOfLifeManager>();
        rng = new System.Random(System.Guid.NewGuid().GetHashCode());
        pendingStartRandomize = randomizeOnStart;
    }

    void Update()
    {
        // Deferred to Update because the manager allocates its arrays in Start,
        // and execution order between the two scripts isn't guaranteed.
        if (pendingStartRandomize && game != null && game.cells != null)
        {
            pendingStartRandomize = false;
            Randomize();
        }

        if (randomizeKey != KeyCode.None && Input.GetKeyDown(randomizeKey)) Randomize();
    }

    // Button-friendly entry point: randomizes at the density set in the Inspector.
    public void Randomize()
    {
        Randomize(density);
    }

    // Randomizes at an explicit fill density (0..1), e.g. a slider's value.
    public void Randomize(float fillDensity)
    {
        if (game == null || game.cells == null)
        {
            Debug.LogWarning("GridRandomizer: no GameOfLifeManager found, or its grid isn't ready yet.");
            return;
        }

        fillDensity = Mathf.Clamp01(fillDensity);
        int palette = Mathf.Clamp(colorCount, 1, 3);
        int n = game.GridSize;

        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                int p = game.Pos(i, j);
                bool alive = rng.NextDouble() < fillDensity;

                game.cells[p] = alive ? 1 : 0;
                game.colorIndex[p] = alive ? rng.Next(palette) : 0;
                game.age[p] = alive ? 1 : 0;
            }
        }

        game.UpdateTilemap();
        if (pauseAfterRandomize) game.SetPaused(true);

        Debug.Log($"Randomized grid: density {fillDensity:0.00}, {game.AliveCount} cells alive.");
    }

    // For a UI Slider's OnValueChanged — changes the density used by Randomize()
    // without regenerating the board right away.
    public void SetDensity(float value)
    {
        density = Mathf.Clamp01(value);
    }

    // For a slider that should redraw the board live while being dragged.
    public void SetDensityAndRandomize(float value)
    {
        SetDensity(value);
        Randomize(density);
    }
}
