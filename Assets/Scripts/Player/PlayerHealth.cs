using System;
using UnityEngine;

/// <summary>
/// PlayerHealth — Player Health Manager
/// Unity 6000.3.8f1 | No deprecated packages
///
/// Sits on the Player root alongside HealthComponent and PlayerMovement.
/// Listens to HealthComponent events and handles player-specific responses:
///   • Disables movement input on death
///   • Triggers death animation on the player's Animator
///   • Fires player-specific death/respawn events for HUD and game manager
///   • Handles respawn logic (timer-based or manual)
///
/// REQUIRES:
///   HealthComponent on the same GameObject (auto-found in Awake)
///   PlayerMovement on the same GameObject (auto-found in Awake)
///   Animator accessible via PlayerMovement.Animator or GetComponentInChildren
///
/// SETUP:
///   1. Add this script to your Player root GameObject
///   2. HealthComponent will be auto-found (or assign in Inspector)
///   3. Configure MaxHealth on the HealthComponent
///   4. Add "Die" Trigger and "IsAlive" Bool to the Player Animator Controller
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class PlayerHealth : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private HealthComponent _health;
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private Animator _animator;

    [Header("Death Settings")]
    [Tooltip("Seconds after death before the respawn sequence begins. 0 = instant.")]
    [SerializeField] private float respawnDelay = 5f;

    [Tooltip("Respawn point. Leave null to respawn at death location.")]
    [SerializeField] private Transform respawnPoint;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Fired when the player dies. Subscribe for screen fade, game over UI, etc.</summary>
    public event Action OnPlayerDied;

    /// <summary>Fired when the player respawns.</summary>
    public event Action OnPlayerRespawned;

    // ─────────────────────────────────────────────────────────────────────────
    //  Animator Parameters (cached hashes)
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int IsAliveHash = Animator.StringToHash("IsAlive");

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _isDead;
    private float _respawnTimer;
    private bool _respawnPending;

    // ── Public pass-throughs so UI can read health without coupling to HealthComponent ──
    public float CurrentHealth => _health != null ? _health.CurrentHealth : 0f;
    public float MaxHealth => _health != null ? _health.MaxHealth : 0f;
    public float HealthPercent => _health != null ? _health.HealthPercent : 0f;
    public bool IsDead => _isDead;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_health == null) _health = GetComponent<HealthComponent>();
        if (_movement == null) _movement = GetComponent<PlayerMovement>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        if (_health == null)
            Debug.LogError("[PlayerHealth] No HealthComponent found on Player!", this);
    }

    private void OnEnable()
    {
        if (_health == null) return;
        _health.OnDied += HandleDeath;
        _health.OnRevived += HandleRevive;
        _health.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (_health == null) return;
        _health.OnDied -= HandleDeath;
        _health.OnRevived -= HandleRevive;
        _health.OnHealthChanged -= HandleHealthChanged;
    }

    private void Update()
    {
        if (!_respawnPending) return;

        _respawnTimer -= Time.deltaTime;
        if (_respawnTimer <= 0f)
        {
            _respawnPending = false;
            ExecuteRespawn();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Event Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleDeath(GameObject source)
    {
        if (_isDead) return;
        _isDead = true;

        if (enableDebugLogs)
            Debug.Log($"[PlayerHealth] Player died. Source: {source?.name ?? "unknown"}", this);

        // ── Disable movement ──────────────────────────────────────────────────
        if (_movement != null)
            _movement.SetMovementEnabled(false);

        // ── Trigger death animation ───────────────────────────────────────────
        if (_animator != null)
        {
            // Only set params that exist in the Animator Controller
            foreach (var param in _animator.parameters)
            {
                if (param.nameHash == IsAliveHash)
                    _animator.SetBool(IsAliveHash, false);
                if (param.nameHash == DieHash)
                    _animator.SetTrigger(DieHash);
            }
        }

        // ── Notify listeners ──────────────────────────────────────────────────
        OnPlayerDied?.Invoke();

        // ── Schedule respawn ──────────────────────────────────────────────────
        if (respawnDelay > 0f)
        {
            _respawnTimer = respawnDelay;
            _respawnPending = true;
        }
        else
        {
            ExecuteRespawn();
        }
    }

    private void HandleRevive()
    {
        _isDead = false;

        if (_movement != null)
            _movement.SetMovementEnabled(true);

        if (_animator != null)
        {
            foreach (var param in _animator.parameters)
            {
                if (param.nameHash == IsAliveHash)
                    _animator.SetBool(IsAliveHash, true);
            }
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        // Forwarded to UI via PlayerHealthUI subscribing to HealthComponent directly
        // No action needed here — kept for future game-logic hooks
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Respawn
    // ─────────────────────────────────────────────────────────────────────────

    private void ExecuteRespawn()
    {
        if (enableDebugLogs)
            Debug.Log("[PlayerHealth] Respawning player.", this);

        // Teleport to respawn point if assigned
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }

        // Restore health (HealthComponent.Revive fires HandleRevive above)
        _health?.Revive(_health.MaxHealth);

        OnPlayerRespawned?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API (callable by future game manager / UI)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Damage the player directly.</summary>
    public void TakeDamage(float amount, GameObject source = null)
        => _health?.TakeDamage(amount, source);

    /// <summary>Heal the player directly.</summary>
    public void Heal(float amount)
        => _health?.Heal(amount);

    /// <summary>Force an immediate respawn regardless of timer.</summary>
    public void ForceRespawn()
    {
        _respawnPending = false;
        ExecuteRespawn();
    }
}