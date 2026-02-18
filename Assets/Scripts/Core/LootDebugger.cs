using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug helper for testing the loot system.
/// Press F6-F9 to spawn test loot sources near the player.
/// Press F10 to log all loot sources in the scene.
/// Attach to the Player GameObject.
/// </summary>
public class LootDebugger : MonoBehaviour
{
    [Header("Test Loot Tables")]
    [SerializeField] private LootTable corpseLootTable;
    [SerializeField] private LootTable chestLootTable;
    [SerializeField] private LootTable herbLootTable;
    [SerializeField] private LootTable miningLootTable;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDistance = 2f;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f6Key.wasPressedThisFrame)
            SpawnTestLootSource(corpseLootTable, LootTableType.Corpse, "Test_Corpse");

        if (Keyboard.current.f7Key.wasPressedThisFrame)
            SpawnTestLootSource(chestLootTable, LootTableType.Chest, "Test_Chest");

        if (Keyboard.current.f8Key.wasPressedThisFrame)
            SpawnTestLootSource(herbLootTable, LootTableType.Herb, "Test_Herb");

        if (Keyboard.current.f9Key.wasPressedThisFrame)
            SpawnTestLootSource(miningLootTable, LootTableType.MiningNode, "Test_MiningNode");

        if (Keyboard.current.f10Key.wasPressedThisFrame)
            LogAllLootSources();
    }

    private void SpawnTestLootSource(LootTable table, LootTableType type, string objName)
    {
        if (table == null)
        {
            Debug.LogWarning($"[LootDebugger] No LootTable assigned for {type}!");
            return;
        }

        Vector3 spawnPos = transform.position + transform.forward * spawnDistance;
        spawnPos.y = transform.position.y;

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objName;
        obj.transform.position = spawnPos;
        obj.transform.localScale = Vector3.one * 0.5f;

        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0)
            obj.layer = layer;
        else
            Debug.LogWarning("[LootDebugger] 'Interactable' layer not found! Add it in Project Settings > Tags and Layers.");

        var lootSource = obj.AddComponent<LootSource>();

        // Set private serialized fields via reflection
        var lootTableField = typeof(LootSource).GetField("lootTable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var typeField = typeof(LootSource).GetField("lootTableType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (lootTableField != null) lootTableField.SetValue(lootSource, table);
        if (typeField != null) typeField.SetValue(lootSource, type);

        Debug.Log($"[LootDebugger] Spawned {objName} at {spawnPos} with LootTable '{table.sourceName}'");
    }

    private void LogAllLootSources()
    {
        var sources = FindObjectsByType<LootSource>(FindObjectsSortMode.None);
        Debug.Log($"[LootDebugger] Found {sources.Length} LootSource(s) in scene:");
        foreach (var s in sources)
        {
            Debug.Log($"  - '{s.gameObject.name}' | Type: {s.Type} | HasLoot: {s.HasLoot} | CanInteract: {s.CanInteract}");
        }
    }
}