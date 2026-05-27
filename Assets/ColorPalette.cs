using UnityEngine;
using UnityEngine.Tilemaps;

// Lets the player pick a paint color by clicking one of the palette sprites.
// The palette buttons are SpriteRenderers (not UI Buttons), so this script does
// its own click detection against each sprite's bounds — no EventSystem,
// Collider2D, or Button wiring required. Works with an orthographic (2D) camera.
//
// Draw-time only: once the simulation runs, cells are recolored by neighbor
// count again (see GameOfLifeManager.UpdateCellTile).
public class ColorPalette : MonoBehaviour
{
    [System.Serializable]
    public class PaintColor
    {
        public string label;            // For Inspector readability, e.g., "Blue"
        public TileBase tile;           // Tile painted while this color is selected
        public SpriteRenderer button;   // The palette sprite the player clicks
    }

    [Tooltip("One entry per palette sprite. Order is its selection index.")]
    [SerializeField] private PaintColor[] colors;

    [Tooltip("Camera that renders the palette sprites. Leave empty to use Camera.main.")]
    [SerializeField] private Camera uiCamera;

    [Tooltip("Scale multiplier applied to the selected sprite so the click is visible.")]
    [SerializeField] private float selectedScale = 1.2f;

    [Tooltip("Color selected automatically when the scene starts (-1 = none).")]
    [SerializeField] private int defaultIndex = 0;

    private int selectedIndex = -1;
    private Vector3[] baseScale;   // Original localScale of each button

    // Tile the user should paint with, or null if nothing is selected
    // (in which case the manager falls back to neighbor-count coloring).
    public TileBase SelectedTile =>
        (selectedIndex >= 0 && selectedIndex < colors.Length) ? colors[selectedIndex].tile : null;

    void Awake()
    {
        if (uiCamera == null) uiCamera = Camera.main;

        // Remember each button's starting scale so the highlight can restore it.
        baseScale = new Vector3[colors.Length];
        for (int i = 0; i < colors.Length; i++)
            if (colors[i].button != null)
                baseScale[i] = colors[i].button.transform.localScale;
    }

    void Start()
    {
        if (defaultIndex >= 0) SelectColor(defaultIndex);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            int hit = ButtonUnderMouse();
            if (hit >= 0) SelectColor(hit);
        }
    }

    // True while the cursor is over any palette sprite. GameOfLifeManager checks
    // this so a click on the palette doesn't also paint a grid cell.
    public bool IsPointerOverPalette() => ButtonUnderMouse() >= 0;

    // Index of the palette sprite under the mouse, or -1 if none.
    private int ButtonUnderMouse()
    {
        if (uiCamera == null) return -1;

        Vector3 wp = uiCamera.ScreenToWorldPoint(Input.mousePosition);
        for (int i = 0; i < colors.Length; i++)
        {
            SpriteRenderer sr = colors[i].button;
            if (sr == null || !sr.enabled) continue;

            // Orthographic 2D: compare X/Y only, ignore the depth (Z) axis.
            Bounds b = sr.bounds;
            if (wp.x >= b.min.x && wp.x <= b.max.x &&
                wp.y >= b.min.y && wp.y <= b.max.y)
                return i;
        }
        return -1;
    }

    // Selects a color by index and highlights its button.
    public void SelectColor(int index)
    {
        selectedIndex = index;

        for (int i = 0; i < colors.Length; i++)
        {
            Transform t = colors[i].button != null ? colors[i].button.transform : null;
            if (t == null) continue;
            t.localScale = (i == index) ? baseScale[i] * selectedScale : baseScale[i];
        }
    }
}
