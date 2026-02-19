using System;
using UnityEngine;

/// <summary>
/// HealthComponent — Generic Health System
/// Unity 6000.3.8f1 | No deprecated packages
///
/// Add this MonoBehaviour to ANY entity that needs health: players, enemies,
/// destructible objects, towers — anything. It owns all health state and fires
/// C# events that other systems subscribe to. It never directly modifies UI,
/// animations, or AI — those systems listen to the events and react.
///
/// IMPLEMENTS: IHealthSystem (loose coupling for damage dealers)
///
/// EVENTS (subscribe in Awake/OnEnable, unsubscribe in OnDisable):
///   OnHealthChanged(current, max)   — fires every time health changes
///   OnDamaged(amount, source)       — fires when TakeDamage is called
///   OnHealed(amount)                — fires when Heal is called
///   OnDied(source)                  — fires once when health hits 0
///   OnRevived()                     — fires when Revive() is called
///
/// SETUP:
///   1. Add HealthComponent to your entity's root GameObject.
///   2. Set MaxHealth in the Inspector.
///   3. Subscribe to events from your animation / UI / AI scripts.
/// </summary>
public class HealthComponent : MonoBehaviour, IHealthSystem
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Health Settings")]
    [Tooltip("Maximum health for this entity.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Starting health. Leave at 0 to start at full MaxHealth.")]
    [SerializeField] private float startingHealth = 0f;

    [Tooltip("If true, entity cannot die (useful for testing or invincible NPCs).")]
    [SerializeField] private bool isInvincible = false;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Events — subscribe in Awake/OnEnable, unsubscribe in OnDisable
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Fired whenever health changes. Parameters: (currentHealth, maxHealth)</summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>Fired when damage is received. Parameters: (damageAmount, sourceGameObject)</summary>
    public event Action<float, GameObject> OnDamaged;

    /// <summary>Fired when health is restored. Parameters: (healAmount)</summary>
    public event Action<float> OnHealed;

    /// <summary>Fired once when health reaches zero. Parameters: (sourceGameObject)</summary>
    public event Action<GameObject> OnDied;

    /// <summary>Fired when Revive() is called on a dead entity.</summary>
    public event Action OnRevived;

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    private float _currentHealth;
    private bool _isDead;

    // ─────────────────────────────────────────────────────────────────────────
    //  IHealthSystem Properties
    // ─────────────────────────────────────────────────────────────────────────

    public float MaxHealth => maxHealth;
    public float CurrentHealth => _currentHealth;
    public bool IsDead => _isDead;

    /// <summary>Normalised health 0–1. Useful for UI sliders.</summary>
    public float HealthPercent => maxHealth > 0f ? _currentHealth / maxHealth : 0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Start at full health unless a starting value was specified
        _currentHealth = startingHealth > 0f ? Mathf.Clamp(startingHealth, 0f, maxHealth) : maxHealth;
        _isDead = _currentHealth <= 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  IHealthSystem Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void TakeDamage(float amount, GameObject source = null)
    {
        if (_isDead || isInvincible || amount <= 0f) return;

        float previous = _currentHealth;
        _currentHealth = Mathf.Max(0f, _currentHealth - amount);

        if (enableDebugLogs)
            Debug.Log($"[HealthComponent] {name} took {amount:F1} dmg from {source?.name ?? "unknown"}. " +
                      $"HP: {previous:F1} → {_currentHealth:F1}", this);

        OnDamaged?.Invoke(amount, source);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth <= 0f)
            Die(source);
    }

    /// <inheritdoc/>
    public void Heal(float amount)
    {
        if (_isDead || amount <= 0f) return;

        float previous = _currentHealth;
        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);

        if (enableDebugLogs)
            Debug.Log($"[HealthComponent] {name} healed {amount:F1}. HP: {previous:F1} → {_currentHealth:F1}", this);

        OnHealed?.Invoke(amount);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    /// <inheritdoc/>
    public void SetHealth(float newHealth, float newMax = -1f)
    {
        if (newMax > 0f)
            maxHealth = newMax;

        _currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);

        if (enableDebugLogs)
            Debug.Log($"[HealthComponent] {name} health forcefully set to {_currentHealth:F1}/{maxHealth:F1}", this);

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    /// <inheritdoc/>
    public void Revive(float healthAmount)
    {
        if (!_isDead) return;

        _isDead = false;
        _currentHealth = Mathf.Clamp(healthAmount, 0f, maxHealth);

        if (enableDebugLogs)
            Debug.Log($"[HealthComponent] {name} revived at {_currentHealth:F1}/{maxHealth:F1}", this);

        OnRevived?.Invoke();
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────────────────────────────────

    private void Die(GameObject source)
    {
        if (_isDead) return; // Guard against double-death

        _isDead = true;

        if (enableDebugLogs)
            Debug.Log($"[HealthComponent] {name} has DIED. Source: {source?.name ?? "unknown"}", this);

        OnDied?.Invoke(source);
        // OnHealthChanged fires so UI can show 0 hp
        OnHealthChanged?.Invoke(0f, maxHealth);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Editor Gizmos
    // ─────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnGUI()
    {
        // Optional: show health above entity head in Scene view during play
    }
#endif
}