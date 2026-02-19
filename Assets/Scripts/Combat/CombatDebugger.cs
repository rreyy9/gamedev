using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CombatDebugger — Health System Test Helper
/// Unity 6000.3.8f1 | No deprecated packages
///
/// Keyboard shortcuts to test health and death without a combat system.
/// Remove before shipping!
///
/// CONTROLS:
///   H     — Deal 10 damage to player
///   J     — Deal 25 damage to player
///   K     — Heal player 20 HP
///   L     — Kill player instantly
///   F11   — Deal 10 damage to ALL enemies in scene
///   F12   — Kill ALL enemies in scene
/// </summary>
public class CombatDebugger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float playerDamageSmall = 10f;
    [SerializeField] private float playerDamageLarge = 25f;
    [SerializeField] private float playerHealAmount = 20f;
    [SerializeField] private float enemyTestDamage = 10f;

    private PlayerHealth _playerHealth;
    private HealthComponent _playerHealthComponent;

    private void Awake()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealth>();
            _playerHealthComponent = player.GetComponent<HealthComponent>();
        }

        if (_playerHealth == null)
            Debug.LogWarning("[CombatDebugger] No PlayerHealth found. Player damage keys won't work.", this);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // ── Player controls ───────────────────────────────────────────────────
        if (keyboard.hKey.wasPressedThisFrame)
        {
            _playerHealthComponent?.TakeDamage(playerDamageSmall, gameObject);
            Debug.Log($"[DEBUG] Dealt {playerDamageSmall} damage to player.");
        }

        if (keyboard.jKey.wasPressedThisFrame)
        {
            _playerHealthComponent?.TakeDamage(playerDamageLarge, gameObject);
            Debug.Log($"[DEBUG] Dealt {playerDamageLarge} damage to player.");
        }

        if (keyboard.kKey.wasPressedThisFrame)
        {
            _playerHealthComponent?.Heal(playerHealAmount);
            Debug.Log($"[DEBUG] Healed player {playerHealAmount} HP.");
        }

        if (keyboard.lKey.wasPressedThisFrame)
        {
            _playerHealthComponent?.TakeDamage(9999f, gameObject);
            Debug.Log("[DEBUG] Killed player.");
        }

        // ── Enemy controls ────────────────────────────────────────────────────
        if (keyboard.f11Key.wasPressedThisFrame)
        {
            var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
                enemy.TakeDamage(enemyTestDamage, gameObject);
            Debug.Log($"[DEBUG] Dealt {enemyTestDamage} damage to {enemies.Length} enemies.");
        }

        if (keyboard.f12Key.wasPressedThisFrame)
        {
            var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
                enemy.TakeDamage(9999f, gameObject);
            Debug.Log($"[DEBUG] Killed {enemies.Length} enemies.");
        }
    }
}