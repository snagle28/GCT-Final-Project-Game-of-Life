using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row of the save panel — the view for a single slot of GridSaveLoad.
//
// It holds its own slot number and exposes no-argument Save/Load/Delete, so a
// row's buttons just take this object in their OnClick: there is no per-button
// slot number to keep in sync, which is the usual way this kind of panel gets
// mis-wired.
//
// Setup per row: add this component, set `slot`, drag in the label, thumbnail
// and the load/delete buttons, then point all three buttons' OnClick at this
// same object.
public class SaveSlotUI : MonoBehaviour
{
    [Tooltip("Leave empty to find the GridSaveLoad in the scene automatically.")]
    [SerializeField] private GridSaveLoad storage;

    [Tooltip("Which save slot this row shows.")]
    [SerializeField] private int slot = 1;

    [Header("Row contents")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private RawImage thumbnail;

    [Tooltip("Greyed out while the slot is empty. Optional.")]
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    // {0} = slot number, {1} = save time, {2} = alive-cell count. Two lines: the
    // row is only ~160px wide once the thumbnail and three buttons have taken
    // their share, which one line of detail text does not fit into.
    [Header("Label text")]
    [SerializeField] private string filledFormat = "SLOT {0}\n{1}   {2} cells";
    [SerializeField] private string emptyFormat = "SLOT {0}\nempty";
    [SerializeField] private string timeFormat = "MM-dd HH:mm";

    void Awake()
    {
        if (storage == null) storage = Object.FindFirstObjectByType<GridSaveLoad>();
    }

    // Refreshing on enable covers the panel being toggled open; the event covers
    // changes made while it is already open.
    void OnEnable()
    {
        if (storage != null) storage.OnSlotsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (storage != null) storage.OnSlotsChanged -= Refresh;
    }

    // ---- Button targets ---------------------------------------------------

    public void Save()
    {
        if (storage != null) storage.SaveToSlot(slot);
    }

    public void Load()
    {
        if (storage != null) storage.LoadFromSlot(slot);
    }

    public void Delete()
    {
        if (storage != null) storage.DeleteSlot(slot);
    }

    // ---- View -------------------------------------------------------------

    public void Refresh()
    {
        if (storage == null) return;

        GridSaveLoad.SlotInfo info = storage.GetSlotInfo(slot);

        if (label != null)
        {
            label.text = info.exists
                ? string.Format(filledFormat, slot, info.savedAtLocal.ToString(timeFormat), info.aliveCount)
                : string.Format(emptyFormat, slot);
        }

        if (thumbnail != null)
        {
            thumbnail.texture = info.thumbnail;
            // Hidden rather than left showing a stale board when the slot is empty.
            thumbnail.enabled = info.exists && info.thumbnail != null;
        }

        if (loadButton != null) loadButton.interactable = info.exists;
        if (deleteButton != null) deleteButton.interactable = info.exists;
    }
}
