using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Back button for the Soundscape gameplay scenes.
//
// Spawns a themed left-arrow icon button (Resources/UI/BackButtonIcon, from the
// 2DSimpleUIPack) in the BOTTOM-RIGHT corner at runtime (no scene wiring) and, on
// click, returns to the Environment-select scene ("Intro Sound") so the player can
// pick a different soundscape. SceneManagement.MoveToSelect() handles the fade and
// jumps straight to the selection loop (skips the replayed intro).
//
// Auto-created on demand by GameOfLifeManager, matching the audio-layer pattern.
public class BackButton : MonoBehaviour
{
    private const string SelectScene = "Intro Sound";
    private const string IconPrefab = "UI/BackButtonIcon";
    private static readonly Vector2 ButtonSize = new Vector2(56f, 56f);
    private static readonly Vector2 CornerInset = new Vector2(-24f, 24f); // from bottom-right

    void Start()
    {
        Canvas canvas = GetOrCreateCanvas();
        EnsureEventSystem();

        Button btn = BuildThemedButton(canvas.transform) ?? BuildFallbackButton(canvas.transform);
        btn.onClick.AddListener(GoBack);
    }

    // Instantiate the themed left-arrow prefab and anchor it to the bottom-right.
    Button BuildThemedButton(Transform parent)
    {
        var prefab = Resources.Load<GameObject>(IconPrefab);
        if (prefab == null) return null;

        var go = Instantiate(prefab, parent, false); // instantiateInWorldSpace: false
        go.name = "BackButton";
        AnchorBottomRight(go.GetComponent<RectTransform>(), ButtonSize);

        var btn = go.GetComponent<Button>();
        return btn != null ? btn : go.AddComponent<Button>();
    }

    // Plain dark rounded button with a "← Back" label, used only if the themed
    // prefab is missing.
    Button BuildFallbackButton(Transform parent)
    {
        var btnGO = new GameObject("BackButton", typeof(RectTransform),
                                   typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, worldPositionStays: false);
        AnchorBottomRight(btnGO.GetComponent<RectTransform>(), new Vector2(160f, 56f));
        btnGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var txtGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txtGO.transform.SetParent(btnGO.transform, worldPositionStays: false);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
        var txt = txtGO.GetComponent<Text>();
        txt.text = "← Back";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 24;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        return btnGO.GetComponent<Button>();
    }

    static void AnchorBottomRight(RectTransform rt, Vector2 size)
    {
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = CornerInset;
        rt.localScale = Vector3.one;
    }

    void GoBack()
    {
        SceneManagement.skipIntro = true;           // ensure skip even via fallback path
        var sm = FindObjectOfType<SceneManagement>();
        if (sm != null) sm.MoveToSelect();          // faded load, jumps to selection
        else SceneManager.LoadScene(SelectScene);   // fallback
    }

    Canvas GetOrCreateCanvas()
    {
        foreach (var c in FindObjectsOfType<Canvas>())
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;

        var go = new GameObject("BackButtonCanvas", typeof(Canvas),
                                typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        return canvas;
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
