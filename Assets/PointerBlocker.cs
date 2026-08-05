using System.Collections.Generic;
using UnityEngine;

// Marks a UI rect that the player must not draw through. GameOfLifeManager
// checks these before painting, so clicking SAVE doesn't also drop a cell on the
// grid behind the panel.
//
// Why not EventSystem.IsPointerOverGameObject()? This scene has several
// full-screen Panel objects that are active with raycastTarget on. A blanket
// check matches those everywhere and disables drawing across the whole board.
// Opting in per-rect means only what is deliberately marked ever blocks.
//
// Add it to any panel or button that should swallow clicks. Only counts while
// the object is active, so a hidden panel blocks nothing.
public class PointerBlocker : MonoBehaviour
{
    private static readonly List<PointerBlocker> active = new List<PointerBlocker>();

    private RectTransform rect;
    private Canvas canvas;

    void Awake()
    {
        rect = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
    }

    void OnEnable()
    {
        if (!active.Contains(this)) active.Add(this);
    }

    void OnDisable()
    {
        active.Remove(this);
    }

    // True while the mouse sits over any active blocker.
    public static bool IsPointerOverAny()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            PointerBlocker blocker = active[i];

            // Destroyed objects can linger if OnDisable never ran (scene unload).
            if (blocker == null)
            {
                active.RemoveAt(i);
                continue;
            }

            if (blocker.ContainsPointer()) return true;
        }
        return false;
    }

    private bool ContainsPointer()
    {
        if (rect == null) return false;

        // Overlay canvases are addressed in screen space; camera-space ones need
        // the canvas camera to convert.
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, cam);
    }
}
