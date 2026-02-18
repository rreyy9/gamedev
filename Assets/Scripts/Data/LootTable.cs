using UnityEngine;
using System;

/// <summary>
/// Defines a single possible drop within a loot table.
/// Each entry has an item, quantity range, and drop chance.
/// </summary>
[Serializable]
public class LootEntry
{
    [Tooltip("The item that can drop")]
    public ItemData item;

    [Tooltip("Minimum quantity when this entry drops")]
    [Min(1)] public int minQuantity = 1;

    [Tooltip("Maximum quantity when this entry drops")]
    [Min(1)] public int maxQuantity = 1;

    [Tooltip("Chance this entry drops (0.0 = never, 1.0 = always)")]
    [Range(0f, 1f)] public float dropChance = 1f;
}

/// <summary>
/// ScriptableObject that defines a loot table — a collection of possible drops.
/// Assign to any Lootable object to define what items it contains.
/// Create via: Right-click > Create > Loot > Loot Table
/// </summary>
[CreateAssetMenu(fileName = "New Loot Table", menuName = "Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    [Tooltip("Display name shown in the loot window title")]
    public string sourceName = "Loot";

    [Tooltip("All possible drops from this source")]
    public LootEntry[] entries;

    [Tooltip("Guaranteed minimum number of entries that will drop")]
    [Min(0)] public int guaranteedDrops = 1;

    /// <summary>
    /// Rolls the loot table and returns the resulting items.
    /// Each entry is independently rolled against its dropChance.
    /// </summary>
    public ItemStack[] GenerateLoot()
    {
        if (entries == null || entries.Length == 0)
            return Array.Empty<ItemStack>();

        var results = new ItemStack[entries.Length];
        int count = 0;
        int guaranteed = 0;

        // First pass: roll each entry
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry.item == null) continue;

            float roll = UnityEngine.Random.value;
            if (roll <= entry.dropChance)
            {
                int qty = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                results[count] = new ItemStack(entry.item, qty);
                count++;
                guaranteed++;
            }
        }

        // Second pass: force entries if we haven't hit guaranteedDrops
        if (guaranteed < guaranteedDrops)
        {
            for (int i = 0; i < entries.Length && guaranteed < guaranteedDrops; i++)
            {
                var entry = entries[i];
                if (entry.item == null) continue;

                bool alreadyAdded = false;
                for (int j = 0; j < count; j++)
                {
                    if (results[j].itemData == entry.item)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    int qty = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                    results[count] = new ItemStack(entry.item, qty);
                    count++;
                    guaranteed++;
                }
            }
        }

        var finalResults = new ItemStack[count];
        Array.Copy(results, finalResults, count);
        return finalResults;
    }
}