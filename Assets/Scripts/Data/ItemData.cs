using UnityEngine;

/// <summary>
/// ScriptableObject that defines the properties of an item.
/// Create instances via: Right-click in Project > Create > Inventory > Item Data
/// Each item in the game has one of these as its "template".
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Display name shown in UI")]
    public string itemName = "New Item";

    [Tooltip("Description shown in tooltips")]
    [TextArea(2, 4)]
    public string description = "";

    [Tooltip("Icon displayed in inventory slots")]
    public Sprite icon;

    [Header("Item Properties")]
    [Tooltip("What category this item belongs to")]
    public ItemCategory category = ItemCategory.Resource;

    [Tooltip("Can this item be stacked?")]
    public bool isStackable = true;

    [Tooltip("Maximum number per stack (only if stackable)")]
    [Min(1)]
    public int maxStackSize = 20;

    [Header("Usage")]
    [Tooltip("Can this item be used/consumed from the action bar?")]
    public bool isConsumable = false;

    [Tooltip("Cooldown in seconds after using this item")]
    [Min(0f)]
    public float useCooldown = 0f;

    [Header("Value")]
    [Tooltip("Sell/buy value for future shop systems")]
    public int goldValue = 0;
}

/// <summary>
/// Categories for organizing and filtering items.
/// </summary>
public enum ItemCategory
{
    Resource,       // Generic quest resources (wood, ore, etc.)
    Consumable,     // Potions, food, scrolls
    QuestItem,      // Items tied to quests (often non-stackable)
    Miscellaneous   // Everything else
}
