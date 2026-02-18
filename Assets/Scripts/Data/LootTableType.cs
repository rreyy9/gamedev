/// <summary>
/// Categories of loot sources in the world.
/// Controls post-loot visual behavior (despawn, disable, hide, respawn).
/// </summary>
public enum LootTableType
{
    /// <summary>NPC corpse — disappears or ragdolls after looting.</summary>
    Corpse,

    /// <summary>Chest or container — plays open animation, can be re-locked.</summary>
    Chest,

    /// <summary>Herb, flower, plant — disappears after harvesting, can respawn.</summary>
    Herb,

    /// <summary>Mining node, rock — depletes after mining, can respawn.</summary>
    MiningNode,

    /// <summary>Generic interactable — custom behavior.</summary>
    Generic
}
