using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// EnemyDebugger — Phase 1 Test Helper
/// 
/// Attach to any GameObject in the scene (e.g. GameManager or a dedicated DebugManager).
/// Uses the New Input System exclusively — zero legacy Input references.
/// 
/// KEYBINDINGS (during Play mode):
///   F6 — Spawn a Hostile enemy near the player
///   F7 — Spawn a Neutral enemy near the player
///   F8 — Spawn a Passive enemy near the player
///   F9 — Log all EnemyControllers in the scene to the Console
///   F10 — Toggle EnableDebugLogs on all enemies
/// 
/// NOTE: This script uses primitive capsules as stand-in enemies.
/// Replace SpawnEnemy() with your actual enemy prefab once it exists.
/// </summary>
public class EnemyDebugger : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("How far from the player the test enemies spawn (metres).")]
    public float SpawnOffset = 4f;

    [Header("References")]
    [Tooltip("Optional: assign your enemy prefab. If null, a capsule placeholder is used.")]
    public GameObject EnemyPrefab;

    private Transform _playerTransform;

    private void Awake()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _playerTransform = playerObj.transform;
        else
            Debug.LogWarning("[EnemyDebugger] No 'Player' tagged object found. Spawn offset will use world origin.");
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f6Key.wasPressedThisFrame) SpawnEnemy(EnemyDisposition.Hostile);
        if (keyboard.f7Key.wasPressedThisFrame) SpawnEnemy(EnemyDisposition.Neutral);
        if (keyboard.f8Key.wasPressedThisFrame) SpawnEnemy(EnemyDisposition.Passive);
        if (keyboard.f9Key.wasPressedThisFrame) LogAllEnemies();
        if (keyboard.f10Key.wasPressedThisFrame) ToggleDebugLogs();
    }

    // ─────────────────────────────────────────────────
    //  Spawn
    // ─────────────────────────────────────────────────

    private void SpawnEnemy(EnemyDisposition disposition)
    {
        // Calculate spawn position: slightly in front and to the side of the player
        Vector3 spawnPos = (_playerTransform != null)
            ? _playerTransform.position + new Vector3(SpawnOffset, 0f, SpawnOffset)
            : new Vector3(SpawnOffset, 0f, SpawnOffset);

        GameObject enemy;

        if (EnemyPrefab != null)
        {
            enemy = Instantiate(EnemyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Placeholder capsule — replaces with real prefab in later phases
            enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.transform.position = spawnPos;

            // Tint based on disposition for easy visual identification
            var renderer = enemy.GetComponent<Renderer>();
            switch (disposition)
            {
                case EnemyDisposition.Hostile:
                    renderer.material.color = Color.red;
                    enemy.name = "DEBUG_Enemy_Hostile";
                    break;
                case EnemyDisposition.Neutral:
                    renderer.material.color = Color.yellow;
                    enemy.name = "DEBUG_Enemy_Neutral";
                    break;
                case EnemyDisposition.Passive:
                    renderer.material.color = Color.gray;
                    enemy.name = "DEBUG_Enemy_Passive";
                    break;
            }
        }

        // Add and configure EnemyController
        var controller = enemy.GetComponent<EnemyController>();
        if (controller == null)
            controller = enemy.AddComponent<EnemyController>();

        controller.Disposition = disposition;
        controller.EnableDebugLogs = true;

        // Face toward the player on spawn
        if (_playerTransform != null)
        {
            Vector3 dir = (_playerTransform.position - enemy.transform.position);
            dir.y = 0f;
            if (dir != Vector3.zero)
                enemy.transform.rotation = Quaternion.LookRotation(dir);
        }

        Debug.Log($"[EnemyDebugger] Spawned {disposition} enemy at {spawnPos}. " +
                  "Use F9 to log all enemies. Walk into its vision cone to test detection.");
    }

    // ─────────────────────────────────────────────────
    //  Logging
    // ─────────────────────────────────────────────────

    private void LogAllEnemies()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        if (enemies.Length == 0)
        {
            Debug.Log("[EnemyDebugger] No EnemyController components found in scene. " +
                      "Press F6/F7/F8 to spawn test enemies.");
            return;
        }

        Debug.Log($"[EnemyDebugger] ── Found {enemies.Length} enemy/enemies ──");
        foreach (var enemy in enemies)
        {
            Debug.Log($"  • {enemy.name} | Disposition: {enemy.Disposition} " +
                      $"| Detection Radius: {enemy.DetectionRadius}m " +
                      $"| Vision Angle: {enemy.VisionAngle}°",
                      enemy);
        }
    }

    private void ToggleDebugLogs()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
            enemy.EnableDebugLogs = !enemy.EnableDebugLogs;

        bool newState = enemies.Length > 0 && enemies[0].EnableDebugLogs;
        Debug.Log($"[EnemyDebugger] Debug logs {(newState ? "ENABLED" : "DISABLED")} on {enemies.Length} enemies.");
    }
}
