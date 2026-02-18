using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// A world object that can be looted by the player.
/// Implements IInteractable for the PlayerInteraction system.
/// Attach to any world object (corpse, chest, herb, mining node).
/// Assign a LootTable ScriptableObject to define possible drops.
/// </summary>
public class LootSource : MonoBehaviour, IInteractable
{
    [Header("Loot Settings")]
    [Tooltip("Loot table that defines possible drops")]
    [SerializeField] private LootTable lootTable;

    [Tooltip("Type of loot source — affects post-loot behavior")]
    [SerializeField] private LootTableType lootTableType = LootTableType.Generic;

    [Tooltip("Custom interaction prompt (leave empty for auto-generated)")]
    [SerializeField] private string customPrompt = "";

    [Header("Respawn Settings")]
    [Tooltip("Whether this source respawns after being looted")]
    [SerializeField] private bool canRespawn = false;

    [Tooltip("Time in seconds before respawning")]
    [SerializeField] private float respawnTime = 30f;

    [Header("Visual Settings")]
    [Tooltip("Optional highlight/glow object to show when interactable")]
    [SerializeField] private GameObject highlightObject;

    [Tooltip("Optional mesh/model to hide when depleted")]
    [SerializeField] private GameObject visualModel;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Runtime state
    private List<ItemStack> currentLoot = new List<ItemStack>();
    private bool hasBeenLooted = false;
    private bool isLootGenerated = false;

    // Events
    public event Action OnLooted;
    public event Action OnRespawned;

    // ─────────────────────────────────────────────
    //  IInteractable Implementation
    // ─────────────────────────────────────────────

    public string InteractionPrompt
    {
        get
        {
            if (!string.IsNullOrEmpty(customPrompt))
                return customPrompt;

            return lootTableType switch
            {
                LootTableType.Corpse => $"Loot {(lootTable != null ? lootTable.sourceName : "Corpse")}",
                LootTableType.Chest => "Open Chest",
                LootTableType.Herb => $"Gather {(lootTable != null ? lootTable.sourceName : "Herb")}",
                LootTableType.MiningNode => $"Mine {(lootTable != null ? lootTable.sourceName : "Node")}",
                _ => "Interact"
            };
        }
    }

    public bool CanInteract => !hasBeenLooted || (isLootGenerated && currentLoot.Count > 0);

    public Transform InteractableTransform => transform;

    public void Interact()
    {
        if (!CanInteract) return;

        if (!isLootGenerated)
        {
            GenerateLoot();
        }

        if (currentLoot.Count == 0)
        {
            if (enableDebugLogs)
                Debug.Log($"[LootSource] '{gameObject.name}' has no loot to show.");
            OnFullyLooted();
            return;
        }

        if (LootUIManager.Instance != null)
        {
            LootUIManager.Instance.OpenLootWindow(this);
        }
        else
        {
            Debug.LogWarning("[LootSource] LootUIManager.Instance is null! Cannot open loot window.");
        }
    }

    public void SetHighlight(bool active)
    {
        if (highlightObject != null)
            highlightObject.SetActive(active);
    }

    // ─────────────────────────────────────────────
    //  Loot Generation
    // ─────────────────────────────────────────────

    private void GenerateLoot()
    {
        isLootGenerated = true;
        currentLoot.Clear();

        if (lootTable == null)
        {
            Debug.LogWarning($"[LootSource] '{gameObject.name}' has no LootTable assigned!");
            return;
        }

        ItemStack[] generated = lootTable.GenerateLoot();

        for (int i = 0; i < generated.Length; i++)
        {
            if (generated[i] != null && generated[i].itemData != null && generated[i].quantity > 0)
            {
                currentLoot.Add(generated[i]);

                if (enableDebugLogs)
                    Debug.Log($"[LootSource] Generated: {generated[i].quantity}x {generated[i].itemData.itemName}");
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[LootSource] '{gameObject.name}' generated {currentLoot.Count} loot entries.");
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    public IReadOnlyList<ItemStack> GetLootContents()
    {
        return currentLoot.AsReadOnly();
    }

    public LootTableType Type => lootTableType;

    public string DisplayName => lootTable != null ? lootTable.sourceName : gameObject.name;

    public bool HasLoot => currentLoot.Count > 0;

    /// <summary>
    /// Removes a specific loot entry by index and transfers it to the player inventory.
    /// Returns overflow count (0 = fully looted, >0 = partial, -1 = invalid).
    /// </summary>
    public int TakeLootItem(int index)
    {
        if (index < 0 || index >= currentLoot.Count)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[LootSource] TakeLootItem: Invalid index {index} (count: {currentLoot.Count})");
            return -1;
        }

        var stack = currentLoot[index];

        if (stack == null)
        {
            Debug.LogError("[LootSource] TakeLootItem: Stack at index is NULL");
            currentLoot.RemoveAt(index);
            return -1;
        }

        if (stack.itemData == null)
        {
            Debug.LogError("[LootSource] TakeLootItem: stack.itemData is NULL");
            currentLoot.RemoveAt(index);
            return -1;
        }

        if (stack.quantity <= 0)
        {
            Debug.LogWarning("[LootSource] TakeLootItem: stack.quantity is 0 or negative");
            currentLoot.RemoveAt(index);
            return 0;
        }

        if (enableDebugLogs)
            Debug.Log($"[LootSource] Attempting to transfer {stack.quantity}x {stack.itemData.itemName}");

        var manager = InventoryManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[LootSource] InventoryManager.Instance is NULL! Item NOT transferred.");
            return stack.quantity;
        }

        int availableSpace = GetAvailableSpaceFor(manager.PlayerInventory, stack.itemData, stack.quantity);
        if (enableDebugLogs)
            Debug.Log($"[LootSource] Available space: {availableSpace} (need: {stack.quantity})");

        if (availableSpace <= 0)
        {
            Debug.LogWarning($"[LootSource] Inventory full! Cannot pick up {stack.itemData.itemName}.");
            return stack.quantity;
        }

        int overflow = manager.AddItemToInventory(stack.itemData, stack.quantity);

        if (enableDebugLogs)
        {
            int transferred = stack.quantity - overflow;
            Debug.Log($"[LootSource] Transferred: {transferred}, Overflow: {overflow}");
        }

        if (overflow <= 0)
        {
            if (enableDebugLogs)
                Debug.Log($"[LootSource] Fully looted {stack.itemData.itemName}");
            currentLoot.RemoveAt(index);
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[LootSource] Partial: {overflow}x {stack.itemData.itemName} remaining");
            stack.quantity = overflow;
            currentLoot[index] = stack;
        }

        if (currentLoot.Count == 0)
        {
            OnFullyLooted();
        }

        return Mathf.Max(overflow, 0);
    }

    /// <summary>
    /// Takes all loot items at once. Returns total overflow count.
    /// </summary>
    public int TakeAllLoot()
    {
        if (enableDebugLogs)
            Debug.Log($"[LootSource] TakeAllLoot: {currentLoot.Count} entries");

        int totalOverflow = 0;

        for (int i = currentLoot.Count - 1; i >= 0; i--)
        {
            int overflow = TakeLootItem(i);

            if (overflow > 0)
            {
                totalOverflow += overflow;
                if (enableDebugLogs)
                    Debug.Log("[LootSource] TakeAllLoot: Inventory full, stopping");
                break;
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[LootSource] TakeAllLoot done. Overflow: {totalOverflow}, Remaining: {currentLoot.Count}");

        return totalOverflow;
    }

    // ─────────────────────────────────────────────
    //  Inventory Space Check
    // ─────────────────────────────────────────────

    private int GetAvailableSpaceFor(Inventory inventory, ItemData item, int desiredQuantity)
    {
        if (inventory == null || item == null) return 0;

        int space = 0;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var slot = inventory.GetSlot(i);

            if (slot == null || !slot.IsValid)
            {
                space += item.isStackable ? item.maxStackSize : 1;
            }
            else if (item.isStackable && slot.itemData == item && !slot.IsFull)
            {
                space += slot.RemainingCapacity;
            }

            if (space >= desiredQuantity)
                return space;
        }

        return space;
    }

    // ─────────────────────────────────────────────
    //  Post-Loot Behavior
    // ─────────────────────────────────────────────

    private void OnFullyLooted()
    {
        if (enableDebugLogs)
            Debug.Log($"[LootSource] '{gameObject.name}' fully looted. Type: {lootTableType}");

        hasBeenLooted = true;
        OnLooted?.Invoke();

        switch (lootTableType)
        {
            case LootTableType.Corpse:
                // Corpses always despawn — no respawn
                if (visualModel != null)
                    visualModel.SetActive(false);
                Destroy(gameObject, 5f);
                break;

            case LootTableType.Chest:
                // Chest stays visible but becomes non-interactable
                // If canRespawn is enabled, hide and respawn with new loot
                if (canRespawn)
                {
                    if (visualModel != null)
                        visualModel.SetActive(false);
                    Invoke(nameof(Respawn), respawnTime);
                }
                break;

            case LootTableType.Herb:
            case LootTableType.MiningNode:
                if (visualModel != null)
                    visualModel.SetActive(false);
                if (canRespawn)
                    Invoke(nameof(Respawn), respawnTime);
                break;

            default:
                if (visualModel != null)
                    visualModel.SetActive(false);
                if (canRespawn)
                    Invoke(nameof(Respawn), respawnTime);
                break;
        }
    }

    private void Respawn()
    {
        if (enableDebugLogs)
            Debug.Log($"[LootSource] '{gameObject.name}' respawning.");

        hasBeenLooted = false;
        isLootGenerated = false;
        currentLoot.Clear();

        if (visualModel != null)
            visualModel.SetActive(true);

        OnRespawned?.Invoke();
    }
}