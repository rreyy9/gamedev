using System;
using UnityEngine;

/// <summary>
/// Core inventory data container with slot management.
/// Handles adding, removing, stacking, splitting, and swapping items.
/// This is purely data/logic — no UI code here.
/// </summary>
public class Inventory
{
    private ItemStack[] slots;
    public int SlotCount => slots.Length;

    /// <summary>
    /// Fired whenever any slot changes. Passes the slot index.
    /// </summary>
    public event Action<int> OnSlotChanged;

    public Inventory(int slotCount)
    {
        slots = new ItemStack[slotCount];
    }

    // ─────────────────────────────────────────────
    //  Read Access
    // ─────────────────────────────────────────────

    /// <summary>
    /// Gets the item stack at the given slot index. Returns null if empty.
    /// </summary>
    public ItemStack GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    /// <summary>
    /// Whether a slot is empty.
    /// </summary>
    public bool IsSlotEmpty(int index)
    {
        if (index < 0 || index >= slots.Length) return true;
        return slots[index] == null || !slots[index].IsValid;
    }

    // ─────────────────────────────────────────────
    //  Add Items
    // ─────────────────────────────────────────────

    /// <summary>
    /// Adds an item to the inventory. Tries to stack first, then uses empty slots.
    /// Returns the number of items that could NOT be added (overflow).
    /// </summary>
    public int AddItem(ItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0) return quantity;

        int remaining = quantity;

        // Phase 1: Try to stack onto existing stacks of the same item
        if (itemData.isStackable)
        {
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i] != null && slots[i].itemData == itemData && !slots[i].IsFull)
                {
                    int canAdd = slots[i].RemainingCapacity;
                    int toAdd = Mathf.Min(remaining, canAdd);
                    slots[i].quantity += toAdd;
                    remaining -= toAdd;
                    OnSlotChanged?.Invoke(i);
                }
            }
        }

        // Phase 2: Place remaining items into empty slots
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (IsSlotEmpty(i))
            {
                int stackSize = itemData.isStackable
                    ? Mathf.Min(remaining, itemData.maxStackSize)
                    : 1;

                slots[i] = new ItemStack(itemData, stackSize);
                remaining -= stackSize;
                OnSlotChanged?.Invoke(i);
            }
        }

        return remaining; // 0 means everything was added
    }

    // ─────────────────────────────────────────────
    //  Remove Items
    // ─────────────────────────────────────────────

    /// <summary>
    /// Removes a quantity of the given item from the inventory.
    /// Removes from the last matching slots first (like WoW).
    /// Returns the number that could NOT be removed.
    /// </summary>
    public int RemoveItem(ItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0) return quantity;

        int remaining = quantity;

        // Remove from last slots first
        for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i] != null && slots[i].itemData == itemData)
            {
                int toRemove = Mathf.Min(remaining, slots[i].quantity);
                slots[i].quantity -= toRemove;
                remaining -= toRemove;

                if (slots[i].quantity <= 0)
                {
                    slots[i] = null;
                }

                OnSlotChanged?.Invoke(i);
            }
        }

        return remaining;
    }

    /// <summary>
    /// Removes a specific quantity from a specific slot.
    /// Returns how many were actually removed.
    /// </summary>
    public int RemoveFromSlot(int index, int quantity)
    {
        if (index < 0 || index >= slots.Length) return 0;
        if (slots[index] == null) return 0;

        int toRemove = Mathf.Min(quantity, slots[index].quantity);
        slots[index].quantity -= toRemove;

        if (slots[index].quantity <= 0)
        {
            slots[index] = null;
        }

        OnSlotChanged?.Invoke(index);
        return toRemove;
    }

    // ─────────────────────────────────────────────
    //  Swap & Move
    // ─────────────────────────────────────────────

    /// <summary>
    /// Swaps the contents of two slots. If both contain the same stackable item,
    /// it will try to merge them instead.
    /// </summary>
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Length) return;
        if (indexB < 0 || indexB >= slots.Length) return;
        if (indexA == indexB) return;

        var stackA = slots[indexA];
        var stackB = slots[indexB];

        // Try to merge if same stackable item
        if (stackA != null && stackB != null
            && stackA.itemData == stackB.itemData
            && stackA.itemData.isStackable)
        {
            int canAdd = stackB.RemainingCapacity;
            int toMove = Mathf.Min(stackA.quantity, canAdd);

            if (toMove > 0)
            {
                stackB.quantity += toMove;
                stackA.quantity -= toMove;

                if (stackA.quantity <= 0)
                {
                    slots[indexA] = null;
                }

                OnSlotChanged?.Invoke(indexA);
                OnSlotChanged?.Invoke(indexB);
                return;
            }
        }

        // Otherwise, plain swap
        slots[indexA] = stackB;
        slots[indexB] = stackA;
        OnSlotChanged?.Invoke(indexA);
        OnSlotChanged?.Invoke(indexB);
    }

    // ─────────────────────────────────────────────
    //  Split Stack
    // ─────────────────────────────────────────────

    /// <summary>
    /// Splits a stack: takes half (rounded down) from sourceIndex and places
    /// it in targetIndex. targetIndex must be empty.
    /// Returns true if the split was successful.
    /// </summary>
    public bool SplitStack(int sourceIndex, int targetIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= slots.Length) return false;
        if (targetIndex < 0 || targetIndex >= slots.Length) return false;
        if (sourceIndex == targetIndex) return false;
        if (IsSlotEmpty(sourceIndex)) return false;
        if (!IsSlotEmpty(targetIndex)) return false;

        var sourceStack = slots[sourceIndex];
        if (sourceStack.quantity <= 1) return false; // Can't split a single item

        int splitAmount = sourceStack.quantity / 2;
        sourceStack.quantity -= splitAmount;

        slots[targetIndex] = new ItemStack(sourceStack.itemData, splitAmount);

        OnSlotChanged?.Invoke(sourceIndex);
        OnSlotChanged?.Invoke(targetIndex);
        return true;
    }

    /// <summary>
    /// Splits a specific amount from source to target.
    /// Target must be either empty or contain the same stackable item with room.
    /// </summary>
    public bool SplitStackAmount(int sourceIndex, int targetIndex, int amount)
    {
        if (sourceIndex < 0 || sourceIndex >= slots.Length) return false;
        if (targetIndex < 0 || targetIndex >= slots.Length) return false;
        if (sourceIndex == targetIndex) return false;
        if (IsSlotEmpty(sourceIndex)) return false;
        if (amount <= 0) return false;

        var sourceStack = slots[sourceIndex];
        if (amount >= sourceStack.quantity) return false;

        // Target is empty: create new stack
        if (IsSlotEmpty(targetIndex))
        {
            sourceStack.quantity -= amount;
            slots[targetIndex] = new ItemStack(sourceStack.itemData, amount);
            OnSlotChanged?.Invoke(sourceIndex);
            OnSlotChanged?.Invoke(targetIndex);
            return true;
        }

        // Target has same item and room
        var targetStack = slots[targetIndex];
        if (targetStack.itemData == sourceStack.itemData && sourceStack.itemData.isStackable)
        {
            int canAdd = targetStack.RemainingCapacity;
            int toMove = Mathf.Min(amount, canAdd);
            if (toMove <= 0) return false;

            targetStack.quantity += toMove;
            sourceStack.quantity -= toMove;

            if (sourceStack.quantity <= 0)
                slots[sourceIndex] = null;

            OnSlotChanged?.Invoke(sourceIndex);
            OnSlotChanged?.Invoke(targetIndex);
            return true;
        }

        return false;
    }

    // ─────────────────────────────────────────────
    //  Queries
    // ─────────────────────────────────────────────

    /// <summary>
    /// Counts the total quantity of a given item across all slots.
    /// </summary>
    public int GetItemCount(ItemData itemData)
    {
        if (itemData == null) return 0;

        int total = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].itemData == itemData)
            {
                total += slots[i].quantity;
            }
        }
        return total;
    }

    /// <summary>
    /// Whether the inventory contains at least the given quantity of an item.
    /// </summary>
    public bool HasItem(ItemData itemData, int quantity = 1)
    {
        return GetItemCount(itemData) >= quantity;
    }

    /// <summary>
    /// Sets a slot directly. Used for drag-and-drop operations.
    /// </summary>
    public void SetSlot(int index, ItemStack stack)
    {
        if (index < 0 || index >= slots.Length) return;
        slots[index] = stack;
        OnSlotChanged?.Invoke(index);
    }

    /// <summary>
    /// Clears a slot completely.
    /// </summary>
    public void ClearSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        slots[index] = null;
        OnSlotChanged?.Invoke(index);
    }
}
