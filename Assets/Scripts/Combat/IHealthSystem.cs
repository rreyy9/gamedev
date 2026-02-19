/// <summary>
/// IHealthSystem — Combat System
/// Unity 6000.3.8f1 | No deprecated packages
///
/// Common contract for any entity that can receive or deal health changes.
/// Implemented by HealthComponent. Damage dealers (attacks, traps, spells)
/// only need this interface — they never reference the concrete MonoBehaviour.
///
/// USAGE EXAMPLE (future attack system):
///   if (other.TryGetComponent(out IHealthSystem target))
///       target.TakeDamage(25f, gameObject);
/// </summary>
public interface IHealthSystem
{
    // ── Properties ────────────────────────────────────────────────────────────
    float MaxHealth { get; }
    float CurrentHealth { get; }
    bool IsDead { get; }

    // ── Methods ───────────────────────────────────────────────────────────────

    /// <summary>Reduce health by <paramref name="amount"/>. Clamped to 0. Triggers OnDied when health hits 0.</summary>
    void TakeDamage(float amount, UnityEngine.GameObject source = null);

    /// <summary>Restore health by <paramref name="amount"/>. Clamped to MaxHealth.</summary>
    void Heal(float amount);

    /// <summary>Instantly set health to <paramref name="newHealth"/> without firing death events.</summary>
    void SetHealth(float newHealth, float newMax = -1f);

    /// <summary>Revive a dead entity, restoring them to <paramref name="healthAmount"/> HP.</summary>
    void Revive(float healthAmount);
}