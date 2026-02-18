using UnityEngine;

/// <summary>
/// Controls the inventory panel UI (the 20-slot grid).
/// Spawns slot UI elements from a prefab and binds them to the inventory data.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform slotContainer;   // The GridLayoutGroup parent
    [SerializeField] private GameObject slotPrefab;      // Prefab for each slot

    private Inventory inventory;
    private InventorySlotUI[] slotUIs;

    /// <summary>
    /// Called by InventoryManager to bind this UI to an inventory.
    /// </summary>
    public void Initialize(Inventory inventory)
    {
        this.inventory = inventory;

        // Clear any existing slots
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        // Create slot UI elements
        slotUIs = new InventorySlotUI[inventory.SlotCount];

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var slotObj = Instantiate(slotPrefab, slotContainer);
            slotObj.name = $"InventorySlot_{i}";

            var slotUI = slotObj.GetComponent<InventorySlotUI>();
            slotUI.Setup(inventory, i, isActionBar: false);
            slotUIs[i] = slotUI;
        }

        // Subscribe to data changes
        inventory.OnSlotChanged += OnSlotDataChanged;
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
        if (inventory != null)
        {
            inventory.OnSlotChanged -= OnSlotDataChanged;
        }
    }
}
