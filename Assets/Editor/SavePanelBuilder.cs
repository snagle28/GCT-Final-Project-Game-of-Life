using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Editor-only generator for the save/load panel.
//
//     Tools > Game of Life > Build Save Panel
//
// Building this by hand means ~20 objects and ~10 OnClick entries, which is slow
// and easy to get subtly wrong (one row's buttons pointing at another row's
// slot, say).
//
// Everything is sized for the Menu Canvas's 800x600 reference resolution and
// drawn with the same 2DSimpleUIPack sprites the scene's own buttons use
// (ui-large-buttons-horizontal_40, swapping to _41 when pressed), so the result
// sits in the existing UI instead of looking bolted on.
//
// Everything it makes is a plain scene object: restyle, move, or delete it by
// hand afterwards, and Ctrl+Z undoes the lot.
public static class SavePanelBuilder
{
    private const string PanelName = "SavePanel";
    private const int SlotCount = 3;

    private const string ButtonSheet = "Assets/Art/2DSimpleUIPack/Examples/Graphics/ui-large-buttons-horizontal.png";
    private const string ButtonNormal = "ui-large-buttons-horizontal_40";
    private const string ButtonPressed = "ui-large-buttons-horizontal_41";

    private const string PanelSheet = "Assets/Art/2DSimpleUIPack/Examples/Graphics/ui-panels.png";
    // _0 is the white frame, which swallows the white label text. _1 is the dark
    // navy one. (_2 red, _3 blue, _4 green, _5 orange.)
    private const string PanelFrame = "ui-frames_1";

    private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    // The pack's sprites are imported at 1 pixel-per-unit while the canvas works
    // at 100, so a sliced border would blow up 100x without this correction.
    // 2px border / (1/100 * 25) = 8px on screen.
    private const float SlicedBorderScale = 25f;

    // The canvas scaler matches WIDTH against an 800x600 reference, so at the
    // game's 16:9 aspect the usable area is 800 x 450 — only 450px of height to
    // spend, not 600. Sizes below are in those reference pixels.
    private const float PanelWidth = 580f;
    private static readonly Vector2 RowButtonSize = new Vector2(54f, 22f);
    private static readonly Vector2 CanvasButtonSize = new Vector2(120f, 40f);

    // The thumbnail is one texture pixel per cell, so it has to be displayed at a
    // whole-number multiple of its own size. Point-filtering a 50x50 board down
    // to, say, 46x46 drops whole rows and columns and the board reads as random
    // static. A 50x50 grid lands on 1:1; smaller grids scale up 2x or 3x.
    private static float ThumbSize
    {
        get
        {
            var game = Object.FindFirstObjectByType<GameOfLifeManager>();
            int cells = game != null ? game.GridSize : 50;
            if (cells <= 0) return 50f;

            int scale = Mathf.Max(1, Mathf.RoundToInt(50f / cells));
            return cells * scale;
        }
    }

    private static readonly Color PanelTint = new Color(1f, 1f, 1f, 1f);
    private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.06f);

    [MenuItem("Tools/Game of Life/Build Save Panel")]
    public static void Build()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Build Save Panel",
                "No Canvas in the open scene.\n\nOpen Soundscape1 (or Soundscape2) first, then run this again.",
                "OK");
            return;
        }

        // Leftovers from an earlier attempt — hand-built or generated — would sit
        // underneath the new panel, so clear them out first (with consent).
        List<GameObject> leftovers = FindLeftovers();
        if (leftovers.Count > 0)
        {
            bool wipe = EditorUtility.DisplayDialog("Build Save Panel",
                "Found save-panel objects already in this scene:\n\n" + Describe(leftovers) +
                "\nDelete them and build a fresh panel?",
                "Delete and rebuild", "Cancel");

            if (!wipe) return;
            DeleteAll(leftovers);
        }

        GridSaveLoad storage = EnsureOnManager<GridSaveLoad>();
        if (storage == null)
        {
            EditorUtility.DisplayDialog("Build Save Panel",
                "No GameOfLifeManager in this scene, so there is no board to save.\n\n" +
                "Open the gameplay scene (Soundscape1 / Soundscape2) and try again.",
                "OK");
            return;
        }
        GridRandomizer randomizer = EnsureOnManager<GridRandomizer>();

        GameObject panel = BuildPanel(canvas.transform, storage);
        BuildCanvasButtons(canvas.transform, panel, randomizer);

        Undo.RegisterCreatedObjectUndo(panel, "Build Save Panel");
        panel.SetActive(false); // starts hidden; the SAVE / LOAD button opens it

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = panel;
        EditorGUIUtility.PingObject(panel);

        Debug.Log($"Save panel built under '{canvas.name}' with {SlotCount} slots, and wired up.\n" +
                  "It starts hidden — press Play and click SAVE / LOAD (bottom right) to open it. " +
                  "To preview it in the editor, tick the SavePanel checkbox at the top of its Inspector.\n" +
                  "Remember to save the scene (Ctrl+S).");
    }

    [MenuItem("Tools/Game of Life/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath + "/");
        Debug.Log($"Save folder: {Application.persistentDataPath}");
    }

    // Removes save-panel objects from the open scene, whether this builder made
    // them or they were assembled by hand. Always asks first and lists exactly
    // what it will delete, because a mis-parented row could otherwise take real
    // UI down with it.
    [MenuItem("Tools/Game of Life/Clean Up Save Panel")]
    public static void CleanUp()
    {
        List<GameObject> leftovers = FindLeftovers();

        if (leftovers.Count == 0)
        {
            EditorUtility.DisplayDialog("Clean Up Save Panel",
                "Nothing to clean up — no SavePanel, save-slot rows, or SAVE / RANDOM buttons in this scene.\n\n" +
                "If you named your panel something else, delete that object by hand; " +
                "its slot rows would have been listed here if it had any.",
                "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog("Clean Up Save Panel",
            "These objects (and everything inside them) will be deleted:\n\n" + Describe(leftovers) +
            "\nThis can be undone with Ctrl+Z.",
            "Delete", "Cancel");

        if (!confirmed) return;

        DeleteAll(leftovers);
        Debug.Log($"Cleaned up {leftovers.Count} save-panel object(s). Save the scene (Ctrl+S) to keep the change.");
    }

    // ---- Panel ------------------------------------------------------------

    private static GameObject BuildPanel(Transform parent, GridSaveLoad storage)
    {
        GameObject panel = NewUI(PanelName, parent);

        var rt = (RectTransform)panel.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(PanelWidth, 0f); // height is driven by the fitter

        BuildBackground(panel);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        // Off, or every child is stretched to the full panel width — which is what
        // made the CLOSE button span the whole panel instead of sitting centred.
        // Rows opt back in individually via flexibleWidth.
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = MakeLabel("Title", panel.transform, "SAVE / LOAD", 16f,
                                   TextAlignmentOptions.Center, 20f);
        SetLayout(title.gameObject, flexibleWidth: 1f);

        for (int slot = 1; slot <= SlotCount; slot++)
            BuildRow(panel.transform, storage, slot);

        // Close button lives inside the panel and hides the panel itself.
        var toggle = panel.AddComponent<SimpleUIToggle>();
        toggle.targetObject = panel;
        Button close = MakeButton("CloseButton", panel.transform, "CLOSE", 11f);
        SetLayout(close.gameObject, preferredWidth: 84f, preferredHeight: 26f, minWidth: 84f);
        UnityEventTools.AddVoidPersistentListener(close.onClick, toggle.HideTarget);

        return panel;
    }

    // The frame art lives on an ignored child rather than on the panel itself.
    // A Sliced Image reports a preferred size of border / pixelsPerUnit, and this
    // pack imports at 1 pixel-per-unit against the canvas's 100, so its 5px
    // border claims 500px — which the ContentSizeFitter would take as the panel
    // height and shove the panel off a 450px-tall screen.
    private static void BuildBackground(GameObject panel)
    {
        GameObject go = NewUI("Background", panel.transform);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.AddComponent<LayoutElement>().ignoreLayout = true;

        var image = go.AddComponent<Image>();
        Sprite frame = LoadSubSprite(PanelSheet, PanelFrame);

        if (frame != null)
        {
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = SlicedBorderScale;
            image.color = PanelTint;
        }
        else
        {
            image.color = new Color(0.09f, 0.09f, 0.12f, 0.96f);
        }
    }

    private static void BuildRow(Transform parent, GridSaveLoad storage, int slot)
    {
        GameObject row = NewUI($"Slot{slot}", parent);
        row.AddComponent<Image>().color = RowColor;

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        float thumbSize = ThumbSize;
        SetLayout(row, preferredHeight: thumbSize + 12f, flexibleWidth: 1f);

        GameObject thumbGo = NewUI("Thumbnail", row.transform);
        RawImage thumb = thumbGo.AddComponent<RawImage>();
        // Exact 1:1 with the texture, and pinned so the layout can't squash it.
        SetLayout(thumbGo, preferredWidth: thumbSize, preferredHeight: thumbSize,
                  minWidth: thumbSize, minHeight: thumbSize);

        TMP_Text label = MakeLabel("Label", row.transform, $"SLOT {slot}", 10f,
                                   TextAlignmentOptions.Left, thumbSize);
        SetLayout(label.gameObject, preferredHeight: thumbSize, flexibleWidth: 1f);

        Button saveBtn   = MakeButton("SaveButton",   row.transform, "SAVE", 10f);
        Button loadBtn   = MakeButton("LoadButton",   row.transform, "LOAD", 10f);
        Button deleteBtn = MakeButton("DeleteButton", row.transform, "DEL",  10f);

        foreach (Button b in new[] { saveBtn, loadBtn, deleteBtn })
            SetLayout(b.gameObject, preferredWidth: RowButtonSize.x, preferredHeight: RowButtonSize.y,
                      minWidth: RowButtonSize.x, minHeight: RowButtonSize.y);

        // The row owns its slot number, so every button just targets this object.
        var slotUI = row.AddComponent<SaveSlotUI>();
        var so = new SerializedObject(slotUI);
        so.FindProperty("storage").objectReferenceValue = storage;
        so.FindProperty("slot").intValue = slot;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("thumbnail").objectReferenceValue = thumb;
        so.FindProperty("loadButton").objectReferenceValue = loadBtn;
        so.FindProperty("deleteButton").objectReferenceValue = deleteBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(saveBtn.onClick, slotUI.Save);
        UnityEventTools.AddVoidPersistentListener(loadBtn.onClick, slotUI.Load);
        UnityEventTools.AddVoidPersistentListener(deleteBtn.onClick, slotUI.Delete);
    }

    // ---- Canvas-level buttons ---------------------------------------------

    // Bottom right, side by side: [SAVE / LOAD] [RANDOM].
    private static void BuildCanvasButtons(Transform canvas, GameObject panel, GridRandomizer randomizer)
    {
        const float margin = 12f;
        const float gap = 8f;

        Button open = MakeButton("SaveLoadButton", canvas, "SAVE / LOAD", 14f);
        PlaceBottomRight((RectTransform)open.transform,
                         new Vector2(-(margin + CanvasButtonSize.x + gap), margin));

        var toggle = open.gameObject.AddComponent<SimpleUIToggle>();
        toggle.targetObject = panel;
        UnityEventTools.AddVoidPersistentListener(open.onClick, toggle.ToggleTarget);
        Undo.RegisterCreatedObjectUndo(open.gameObject, "Build Save Panel");

        if (randomizer != null)
        {
            Button random = MakeButton("RandomButton", canvas, "RANDOM", 14f);
            PlaceBottomRight((RectTransform)random.transform, new Vector2(-margin, margin));
            UnityEventTools.AddVoidPersistentListener(random.onClick, randomizer.Randomize);
            Undo.RegisterCreatedObjectUndo(random.gameObject, "Build Save Panel");
        }
    }

    private static void PlaceBottomRight(RectTransform rt, Vector2 offset)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = CanvasButtonSize;
    }

    // ---- Builders ---------------------------------------------------------

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TMP_Text MakeLabel(string name, Transform parent, string text, float size,
                                      TextAlignmentOptions align, float height)
    {
        GameObject go = NewUI(name, parent);
        TMP_Text tmp = ConfigureText(go, text, size, align);
        SetLayout(go, preferredHeight: height);
        return tmp;
    }

    // Built from the scene's own button sprite rather than a prefab: the pack's
    // prefabs carry legacy UI Text and a 48x16 layout, and the text child needs
    // to stretch edge to edge or short labels wrap one letter per line.
    private static Button MakeButton(string name, Transform parent, string text, float fontSize)
    {
        GameObject go = NewUI(name, parent);

        var image = go.AddComponent<Image>();
        Sprite normal = LoadSubSprite(ButtonSheet, ButtonNormal);
        Sprite pressed = LoadSubSprite(ButtonSheet, ButtonPressed);

        if (normal != null)
        {
            image.sprite = normal;
            image.type = Image.Type.Simple;
        }
        else
        {
            Debug.LogWarning($"SavePanelBuilder: '{ButtonNormal}' not found in {ButtonSheet}; " +
                             "falling back to the stock Unity button sprite.");
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
        }

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        if (normal != null && pressed != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.highlightedSprite = normal;
            state.selectedSprite = normal;
            state.pressedSprite = pressed;
            state.disabledSprite = pressed; // reads as "sunken", i.e. unavailable
            button.spriteState = state;
        }

        GameObject labelGo = NewUI("Text", go.transform);
        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        TMP_Text tmp = ConfigureText(labelGo, text, fontSize, TextAlignmentOptions.Center);
        tmp.margin = Vector4.zero;
        tmp.raycastTarget = false;

        return button;
    }

    private static TMP_Text ConfigureText(GameObject go, string text, float size, TextAlignmentOptions align)
    {
        var tmp = go.AddComponent<TextMeshProUGUI>();

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font != null) tmp.font = font;

        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = align;
        return tmp;
    }

    private static Sprite LoadSubSprite(string assetPath, string spriteName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
            if (asset is Sprite sprite && sprite.name == spriteName) return sprite;

        return null;
    }

    private static void SetLayout(GameObject go, float preferredWidth = -1f,
                                  float preferredHeight = -1f, float flexibleWidth = -1f,
                                  float minWidth = -1f, float minHeight = -1f)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null) element = go.AddComponent<LayoutElement>();

        if (preferredWidth  >= 0f) element.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
        if (flexibleWidth   >= 0f) element.flexibleWidth = flexibleWidth;
        if (minWidth        >= 0f) element.minWidth = minWidth;
        if (minHeight       >= 0f) element.minHeight = minHeight;
    }

    // ---- Cleanup helpers --------------------------------------------------

    // Deliberately conservative: it matches the objects this builder creates and
    // the names the manual instructions used, and it never walks *up* to a
    // parent. A row dropped inside some pre-existing panel is removed on its
    // own rather than taking that panel with it.
    private static List<GameObject> FindLeftovers()
    {
        var found = new List<GameObject>();

        foreach (var row in Object.FindObjectsByType<SaveSlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!found.Contains(row.gameObject)) found.Add(row.gameObject);

        foreach (string name in new[] { PanelName, "SaveLoadButton", "RandomButton" })
            foreach (GameObject go in FindByName(name))
                if (!found.Contains(go)) found.Add(go);

        // Drop anything already covered by an ancestor in the list, so deleting
        // a panel doesn't leave dangling references to its own rows.
        var roots = new List<GameObject>();
        foreach (GameObject go in found)
            if (!HasAncestorIn(go, found)) roots.Add(go);

        return roots;
    }

    private static List<GameObject> FindByName(string name)
    {
        var matches = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectByName(root.transform, name, matches);
        return matches;
    }

    private static void CollectByName(Transform t, string name, List<GameObject> into)
    {
        if (t.name == name) into.Add(t.gameObject);
        foreach (Transform child in t) CollectByName(child, name, into);
    }

    private static bool HasAncestorIn(GameObject go, List<GameObject> candidates)
    {
        for (Transform t = go.transform.parent; t != null; t = t.parent)
            if (candidates.Contains(t.gameObject)) return true;
        return false;
    }

    private static string Describe(List<GameObject> objects)
    {
        var text = new System.Text.StringBuilder();
        foreach (GameObject go in objects) text.AppendLine("  • " + HierarchyPath(go));
        return text.ToString();
    }

    private static string HierarchyPath(GameObject go)
    {
        string path = go.name;
        for (Transform t = go.transform.parent; t != null; t = t.parent) path = t.name + " / " + path;
        return path;
    }

    private static void DeleteAll(List<GameObject> objects)
    {
        foreach (GameObject go in objects)
            if (go != null) Undo.DestroyObjectImmediate(go);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // ---- Scene lookups ----------------------------------------------------

    private static Canvas FindCanvas()
    {
        Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        foreach (Canvas c in all)
            if (c.name == "Menu Canvas") return c;

        foreach (Canvas c in all)
            if (c.transform.parent == null) return c;

        return all.Length > 0 ? all[0] : null;
    }

    private static T EnsureOnManager<T>() where T : Component
    {
        var manager = Object.FindFirstObjectByType<GameOfLifeManager>();
        if (manager == null) return null;

        T component = manager.GetComponent<T>();
        if (component == null)
        {
            component = Undo.AddComponent<T>(manager.gameObject);
            Debug.Log($"SavePanelBuilder: added {typeof(T).Name} to '{manager.name}'.");
        }
        return component;
    }
}
