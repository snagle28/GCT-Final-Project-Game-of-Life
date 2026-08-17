using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// Saves and restores whole boards — cell life, color, and age — across numbered
// slots. This is the storage layer only: it knows nothing about UI. SaveSlotUI
// drives one panel row per slot on top of it.
//
// Each slot writes two files into Application.persistentDataPath:
//   gol_save_{slot}.json  — the board data
//   gol_save_{slot}.png   — a thumbnail, one pixel per cell
// On macOS that folder is ~/Library/Application Support/<company>/<product>/.
// Use the "Open Save Folder" context-menu item on this component to jump there.
//
// Files rather than PlayerPrefs: a 50x50 board is 2500 cells across three arrays,
// which PlayerPrefs (a string store) handles badly, and files can be shared
// between teammates.
public class GridSaveLoad : MonoBehaviour
{
    // On-disk shape. JsonUtility needs public fields on a [Serializable] type.
    [System.Serializable]
    private class GridSave
    {
        public int gridSize;
        public int[] cells;
        public int[] colorIndex;
        public int[] age;
        public string savedAtUtc;
    }

    // What the UI needs to describe a slot without loading the board into play.
    public struct SlotInfo
    {
        public bool exists;
        public System.DateTime savedAtLocal;
        public int aliveCount;
        public Texture2D thumbnail;
    }

    [Tooltip("Leave empty to find the GameOfLifeManager in the scene automatically.")]
    [SerializeField] private GameOfLifeManager game;

    [Tooltip("How many save slots exist. Add one panel row per slot.")]
    [SerializeField, Range(1, 10)] private int slotCount = 3;

    [Tooltip("Slot used by the Save()/Load() shortcuts below.")]
    [SerializeField] private int defaultSlot = 1;

    // Letter keys, not F5/F9: on macOS the function row is claimed by the system
    // (dictation, Mission Control) unless the user has turned on "Use F1, F2,
    // etc. as standard function keys", so those presses never reach Unity.
    // S and L are free — the manager's shortcuts are Space/C/G/B/T/M/D/P and the
    // palette owns 1/2/3.
    [Tooltip("Quick-save / quick-load keys for the default slot. None disables.")]
    [SerializeField] private KeyCode saveKey = KeyCode.S;
    [SerializeField] private KeyCode loadKey = KeyCode.L;

    [Tooltip("Pause the simulation after loading so the board can be inspected.")]
    [SerializeField] private bool pauseAfterLoad = true;

    // Thumbnails can't read their colors off the tiles: each soundscape uses a
    // different tile set, and sampling a TileBase's sprite needs read/write
    // enabled on the texture. Setting them per scene here is the sturdy option.
    [Header("Thumbnail colors (match this scene's tile set)")]
    [SerializeField] private Color[] thumbnailColors =
    {
        new Color(0.949f, 0.843f, 0.306f), // 0 = Yellow / C
        new Color(0.910f, 0.851f, 0.710f), // 1 = Beige  / F
        new Color(0.357f, 0.639f, 0.878f), // 2 = Blue   / G
    };
    [SerializeField] private Color thumbnailBackground = new Color(0.102f, 0.102f, 0.125f, 1f);

    // Raised whenever a slot's contents change, so open panel rows can refresh.
    public event System.Action OnSlotsChanged;

    public int SlotCount => slotCount;
    public string SaveDirectory => Application.persistentDataPath;

    // Reading a slot means parsing its JSON, so results are cached and only
    // dropped when that slot is written or deleted.
    private readonly Dictionary<int, SlotInfo> cache = new Dictionary<int, SlotInfo>();

    void Awake()
    {
        if (game == null) game = Object.FindFirstObjectByType<GameOfLifeManager>();
    }

    // Keeps the quick-save slot inside the range the panel actually shows, so a
    // stray value can't write a save no row can reach.
    void OnValidate()
    {
        defaultSlot = Mathf.Clamp(defaultSlot, 1, Mathf.Max(1, slotCount));
    }

    void Update()
    {
        if (saveKey != KeyCode.None && Input.GetKeyDown(saveKey)) Save();
        if (loadKey != KeyCode.None && Input.GetKeyDown(loadKey)) Load();
    }

    void OnDestroy()
    {
        foreach (var entry in cache)
            if (entry.Value.thumbnail != null) Destroy(entry.Value.thumbnail);
        cache.Clear();
    }

    // ---- Public API -------------------------------------------------------

    public void Save() => SaveToSlot(defaultSlot);
    public void Load() => LoadFromSlot(defaultSlot);

    public void SaveToSlot(int slot)
    {
        if (!GridReady("save")) return;

        var data = new GridSave
        {
            gridSize   = game.GridSize,
            cells      = (int[])game.cells.Clone(),
            colorIndex = (int[])game.colorIndex.Clone(),
            age        = (int[])game.age.Clone(),
            savedAtUtc = System.DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
        };

        // Built before writing so a failed write leaves no half-updated cache.
        Texture2D thumb = BuildThumbnail();

        try
        {
            File.WriteAllText(JsonPath(slot), JsonUtility.ToJson(data));
            File.WriteAllBytes(PngPath(slot), thumb.EncodeToPNG());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GridSaveLoad: couldn't write slot {slot} — {e.Message}");
            Destroy(thumb);
            return;
        }

        // Reuse the texture we just built instead of re-reading the PNG.
        DropCached(slot);
        cache[slot] = new SlotInfo
        {
            exists        = true,
            savedAtLocal  = System.DateTime.UtcNow.ToLocalTime(),
            aliveCount    = game.AliveCount,
            thumbnail     = thumb,
        };

        Debug.Log($"Saved grid to slot {slot} ({game.AliveCount} alive) → {JsonPath(slot)}");
        OnSlotsChanged?.Invoke();
    }

    public bool LoadFromSlot(int slot)
    {
        if (!GridReady("load")) return false;

        string path = JsonPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"GridSaveLoad: slot {slot} is empty, nothing to load.");
            return false;
        }

        GridSave data;
        try
        {
            data = JsonUtility.FromJson<GridSave>(File.ReadAllText(path));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GridSaveLoad: slot {slot} couldn't be read — {e.Message}");
            return false;
        }

        if (data == null || data.cells == null || data.gridSize <= 0)
        {
            Debug.LogError($"GridSaveLoad: slot {slot} is corrupt; leaving the board untouched.");
            return false;
        }

        Apply(data);
        game.UpdateTilemap();
        if (pauseAfterLoad) game.SetPaused(true);

        SlotInfo info = GetSlotInfo(slot);
        Debug.Log($"Loaded grid from slot {slot} (saved {info.savedAtLocal:yyyy-MM-dd HH:mm}, {game.AliveCount} alive).");
        return true;
    }

    public bool HasSlot(int slot) => GetSlotInfo(slot).exists;

    public void DeleteSlot(int slot)
    {
        try
        {
            if (File.Exists(JsonPath(slot))) File.Delete(JsonPath(slot));
            if (File.Exists(PngPath(slot)))  File.Delete(PngPath(slot));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GridSaveLoad: couldn't delete slot {slot} — {e.Message}");
            return;
        }

        DropCached(slot);
        Debug.Log($"Deleted save slot {slot}.");
        OnSlotsChanged?.Invoke();
    }

    // Slot summary for the UI. Cheap after the first call thanks to the cache.
    public SlotInfo GetSlotInfo(int slot)
    {
        if (cache.TryGetValue(slot, out SlotInfo cached)) return cached;

        var info = new SlotInfo();
        string path = JsonPath(slot);

        if (File.Exists(path))
        {
            try
            {
                var data = JsonUtility.FromJson<GridSave>(File.ReadAllText(path));
                if (data != null && data.cells != null)
                {
                    int alive = 0;
                    for (int k = 0; k < data.cells.Length; k++) alive += data.cells[k] == 1 ? 1 : 0;

                    info.exists       = true;
                    info.aliveCount   = alive;
                    info.savedAtLocal = ParseSavedAt(data.savedAtUtc);
                    info.thumbnail    = LoadThumbnail(slot);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"GridSaveLoad: slot {slot} couldn't be inspected — {e.Message}");
            }
        }

        cache[slot] = info;
        return info;
    }

    [ContextMenu("Open Save Folder")]
    private void OpenSaveFolder() => Application.OpenURL("file://" + Application.persistentDataPath);

    // ---- Internals --------------------------------------------------------

    private string JsonPath(int slot) => Path.Combine(Application.persistentDataPath, $"gol_save_{slot}.json");
    private string PngPath(int slot)  => Path.Combine(Application.persistentDataPath, $"gol_save_{slot}.png");

    private bool GridReady(string action)
    {
        if (game == null || game.cells == null)
        {
            Debug.LogWarning($"GridSaveLoad: can't {action} — no GameOfLifeManager, or its grid isn't ready yet.");
            return false;
        }
        return true;
    }

    // Writes the save into the live board. A save from a differently sized grid
    // is honoured as far as it overlaps rather than rejected outright.
    private void Apply(GridSave data)
    {
        System.Array.Clear(game.cells, 0, game.cells.Length);
        System.Array.Clear(game.colorIndex, 0, game.colorIndex.Length);
        System.Array.Clear(game.age, 0, game.age.Length);

        int n = game.GridSize;
        int m = data.gridSize;
        int copy = Mathf.Min(n, m);

        if (m != n)
            Debug.LogWarning($"GridSaveLoad: save is {m}x{m} but this scene's grid is {n}x{n}; " +
                             $"restoring the overlapping {copy}x{copy} corner only.");

        bool hasColor = data.colorIndex != null && data.colorIndex.Length >= m * m;
        bool hasAge   = data.age        != null && data.age.Length        >= m * m;

        for (int j = 0; j < copy; j++)
        {
            for (int i = 0; i < copy; i++)
            {
                int src = i + j * m;
                if (src >= data.cells.Length) continue;

                int dst = game.Pos(i, j);
                game.cells[dst]      = data.cells[src] == 1 ? 1 : 0;
                game.colorIndex[dst] = hasColor ? Mathf.Clamp(data.colorIndex[src], 0, 2) : 0;
                game.age[dst]        = hasAge ? data.age[src] : game.cells[dst];
            }
        }
    }

    // One pixel per cell. Unity textures start at the bottom-left and index as
    // x + y*width, which is exactly the manager's Pos(i, j) = i + j*gridSize —
    // so no flip is needed.
    private Texture2D BuildThumbnail()
    {
        int n = game.GridSize;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var pixels = new Color32[n * n];

        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                int p = game.Pos(i, j);
                pixels[i + j * n] = game.cells[p] == 1
                    ? ThumbnailColor(game.colorIndex[p])
                    : thumbnailBackground;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point; // keep the cells crisp, not blurred
        return tex;
    }

    private Color ThumbnailColor(int colorIndex)
    {
        if (thumbnailColors != null && colorIndex >= 0 && colorIndex < thumbnailColors.Length)
            return thumbnailColors[colorIndex];
        return Color.white;
    }

    private Texture2D LoadThumbnail(int slot)
    {
        string path = PngPath(slot);
        if (!File.Exists(path)) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(path)))
        {
            Destroy(tex);
            return null;
        }

        tex.filterMode = FilterMode.Point; // set after LoadImage, which resizes
        return tex;
    }

    private void DropCached(int slot)
    {
        if (cache.TryGetValue(slot, out SlotInfo old) && old.thumbnail != null) Destroy(old.thumbnail);
        cache.Remove(slot);
    }

    private static System.DateTime ParseSavedAt(string savedAtUtc)
    {
        if (!string.IsNullOrEmpty(savedAtUtc) &&
            System.DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture,
                                     DateTimeStyles.RoundtripKind, out System.DateTime parsed))
            return parsed.ToLocalTime();

        return System.DateTime.MinValue;
    }
}
