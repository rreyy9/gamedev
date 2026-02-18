using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug helper to test the inventory system.
/// Provides keyboard shortcuts to add test items.
/// Remove this script before shipping!
/// </summary>
public class InventoryDebugger : MonoBehaviour
{
    [Header("Test Items (Assign in Inspector)")]
    [SerializeField] private ItemData[] testItems;

    [Header("Settings")]
    [SerializeField] private int addQuantity = 5;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // F1-F4: Add test items to inventory
        if (testItems != null)
        {
            Key[] fKeys = { Key.F1, Key.F2, Key.F3, Key.F4 };

            for (int i = 0; i < Mathf.Min(testItems.Length, 4); i++)
            {
                if (keyboard[fKeys[i]].wasPressedThisFrame && testItems[i] != null)
                {
                    var manager = InventoryManager.Instance;
                    if (manager != null)
                    {
                        manager.AddItemToInventory(testItems[i], addQuantity);
                        Debug.Log($"[DEBUG] Added {addQuantity}x {testItems[i].itemName}");
                    }
                }
            }
        }

        // F5: Log inventory contents
        if (keyboard[Key.F5].wasPressedThisFrame)
        {
            LogInventory();
        }
    }

    private void LogInventory()
    {
        var manager = InventoryManager.Instance;
        if (manager == null) return;

        Debug.Log("═══ INVENTORY CONTENTS ═══");
        for (int i = 0; i < manager.PlayerInventory.SlotCount; i++)
        {
            var stack = manager.PlayerInventory.GetSlot(i);
            if (stack != null && stack.IsValid)
            {
                Debug.Log($"  Slot {i}: {stack.quantity}x {stack.itemData.itemName}");
            }
        }

        Debug.Log("═══ ACTION BAR CONTENTS ═══");
        for (int i = 0; i < manager.ActionBar.SlotCount; i++)
        {
            var stack = manager.ActionBar.GetSlot(i);
            if (stack != null && stack.IsValid)
            {
                Debug.Log($"  Slot {i}: {stack.quantity}x {stack.itemData.itemName}");
            }
        }
    }
}