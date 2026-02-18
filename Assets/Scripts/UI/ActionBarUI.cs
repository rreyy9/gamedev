using UnityEngine;
using TMPro;

/// <summary>
/// Controls the action bar UI (the 9-slot bar at the bottom of the screen).
/// Always visible. Supports number key (1-9) activation and selection highlight.
/// </summary>
public class ActionBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform slotContainer;   // The HorizontalLayoutGroup parent
    [SerializeField] private GameObject slotPrefab;      // Prefab for each action bar slot
    [SerializeField] private GameObject keyLabelPrefab;  // Optional: prefab for the "1"-"9" labels

    private Inventory actionBar;
    private InventorySlotUI[] slotUIs;
    private int selectedIndex = 0;

    /// <summary>
    /// Called by InventoryManager to bind this UI to the action bar inventory.
    /// </summary>
    public void Initialize(Inventory actionBar)
    {
        this.actionBar = actionBar;

        // Clear any existing slots
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        // Create slot UI elements
        slotUIs = new InventorySlotUI[actionBar.SlotCount];

        for (int i = 0; i < actionBar.SlotCount; i++)
        {
            var slotObj = Instantiate(slotPrefab, slotContainer);
            slotObj.name = $"ActionBarSlot_{i}";

            var slotUI = slotObj.GetComponent<InventorySlotUI>();
            slotUI.Setup(actionBar, i, isActionBar: true);
            slotUIs[i] = slotUI;

            // Add key number label (1-9)
            AddKeyLabel(slotObj.transform, i + 1);
        }

        // Subscribe to data changes
        actionBar.OnSlotChanged += OnSlotDataChanged;

        // Highlight the first slot by default
        UpdateSelection(0);
    }

    /// <summary>
    /// Adds a small number label (1-9) to the corner of the slot.
    /// </summary>
    private void AddKeyLabel(Transform slotTransform, int keyNumber)
    {
        // Create a text label in the top-left corner of the slot
        var labelObj = new GameObject($"KeyLabel_{keyNumber}");
        labelObj.transform.SetParent(slotTransform, false);

        var rectTransform = labelObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(2f, -2f);
        rectTransform.sizeDelta = new Vector2(16f, 16f);

        var text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = keyNumber.ToString();
        text.fontSize = 10f;
        text.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
    }

    /// <summary>
    /// Updates which slot is visually selected.
    /// </summary>
    public void UpdateSelection(int newIndex)
    {
        if (slotUIs == null) return;

        // Deselect previous
        if (selectedIndex >= 0 && selectedIndex < slotUIs.Length)
        {
            slotUIs[selectedIndex].SetSelected(false);
        }

        selectedIndex = Mathf.Clamp(newIndex, 0, slotUIs.Length - 1);

        // Select new
        slotUIs[selectedIndex].SetSelected(true);
    }

    private void OnSlotDataChanged(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotUIs.Length && slotUIs[slotIndex] != null)
        {
            slotUIs[slotIndex].Refresh();
        }
    }

    private void OnDestroy()
    {
        if (actionBar != null)
        {
            actionBar.OnSlotChanged -= OnSlotDataChanged;
        }
    }
}
