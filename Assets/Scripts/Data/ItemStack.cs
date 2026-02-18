using System;

/// <summary>
/// Represents a stack of items in an inventory slot.
/// This is the runtime data - an ItemData reference + a quantity.
/// </summary>
[Serializable]
public class ItemStack
{
    public ItemData itemData;
    public int quantity;

    public ItemStack(ItemData itemData, int quantity)
    {
        this.itemData = itemData;
        this.quantity = quantity;
    }

    /// <summary>
    /// Returns a copy of this stack.
    /// </summary>
    public ItemStack Clone()
    {
        return new ItemStack(itemData, quantity);
    }

    /// <summary>
    /// Whether this stack is at maximum capacity.
    /// </summary>
    public bool IsFull => itemData != null && quantity >= itemData.maxStackSize;

    /// <summary>
    /// How many more items can fit in this stack.
    /// </summary>
    public int RemainingCapacity => itemData != null ? itemData.maxStackSize - quantity : 0;

    /// <summary>
    /// Whether this is a valid, non-empty stack.
    /// </summary>
    public bool IsValid => itemData != null && quantity > 0;
}
