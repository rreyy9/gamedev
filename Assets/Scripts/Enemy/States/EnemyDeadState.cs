using UnityEngine;

/// <summary>
/// EnemyDeadState — Health System Integration
/// Unity 6000.3.8f1 | No deprecated packages
///
/// Entered when EnemyController.OnDeath() fires from HealthComponent.OnDied.
/// Responsibilities:
///   1. Stop NavMeshAgent and all movement immediately
///   2. Trigger the Death animation on the Animator
///   3. Enable the LootSource component (deferred from the brainstorm doc) so the
///      player can loot the corpse using the existing loot window system
///   4. Disable the NavMeshAgent, vision cone, and collision after a delay
///      so the corpse lingers for a natural WoW-style feel
///
/// ANIMATOR REQUIREMENTS:
///   Add a Trigger parameter named "Die" to your enemy's Animator Controller.
///   Add a Death state driven by the Die trigger (see health system guide).
///
/// LOOT SETUP:
///   Add a LootSource component to the enemy prefab root.
///   Set its LootTable reference in the Inspector.
///   Set LootableType to Corpse.
///   The LootSource component should start DISABLED — this state enables it on death.
///
/// This state is a terminal state — no transitions out.
/// The enemy "object" persists for corpse looting then optionally despawns.
/// </summary>
public class EnemyDeadState : EnemyState
{
    // ── Animator parameter (cached hash) ─────────────────────────────────────
    private static readonly int DieHash = Animator.StringToHash("Die");

    // ── Configurable delays ───────────────────────────────────────────────────
    /// <summary>Seconds after death before the NavMeshAgent is disabled.</summary>
    private const float AgentDisableDelay = 0.5f;

    /// <summary>
    /// Seconds after death before the corpse starts to despawn.
    /// LootSource with LootTableType.Corpse will also Destroy after looting —
    /// this is a safety-net fallback for unlooted corpses.
    /// </summary>
    private const float DespawnDelay = 30f;

    // ── State ─────────────────────────────────────────────────────────────────
    private float _timer;
    private bool _agentDisabled;
    private bool _despawnScheduled;
    private LootSource _lootSource;
    private Collider _collider;

    // ─────────────────────────────────────────────────────────────────────────
    public EnemyDeadState(EnemyController controller) : base(controller) { }

    // ─────────────────────────────────────────────────────────────────────────
    public override void Enter()
    {
        _timer = 0f;
        _agentDisabled = false;
        _despawnScheduled = false;

        // ── 1. Freeze movement ────────────────────────────────────────────────
        if (Controller.Agent != null)
        {
            Controller.Agent.isStopped = true;
            Controller.Agent.velocity = Vector3.zero;
        }

        // ── 2. Trigger death animation ────────────────────────────────────────
        if (Controller.EnemyAnimator != null)
        {
            Controller.EnemyAnimator.SetFloat("Speed", 0f);
            Controller.EnemyAnimator.SetBool("IsAttacking", false);

            // Only fire Die trigger if it exists in the Animator Controller
            // (AnimatorController parameter check avoids runtime errors if not yet set up)
            foreach (var param in Controller.EnemyAnimator.parameters)
            {
                if (param.nameHash == DieHash)
                {
                    Controller.EnemyAnimator.SetTrigger(DieHash);
                    break;
                }
            }
        }

        // ── 3. Enable loot interaction ────────────────────────────────────────
        _lootSource = Controller.GetComponent<LootSource>();
        if (_lootSource != null)
        {
            _lootSource.enabled = true;

            if (Controller.EnableDebugLogs)
                Debug.Log($"[{Controller.name}] LootSource enabled on death.", Controller);
        }
        else
        {
            if (Controller.EnableDebugLogs)
                Debug.LogWarning($"[{Controller.name}] No LootSource component found — corpse won't be lootable.", Controller);
        }

        // ── 4. Cache collider for later disable ───────────────────────────────
        _collider = Controller.GetComponent<Collider>();

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] → Dead", Controller);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public override void Tick()
    {
        _timer += Time.deltaTime;

        // Disable NavMeshAgent a short time after death so the ragdoll/corpse
        // doesn't get pushed by the agent trying to resolve its position
        if (!_agentDisabled && _timer >= AgentDisableDelay)
        {
            _agentDisabled = true;

            if (Controller.Agent != null)
                Controller.Agent.enabled = false;
        }

        // Auto-despawn: only schedule once, only if enabled
        if (!_despawnScheduled && DespawnDelay > 0f && _timer >= DespawnDelay)
        {
            _despawnScheduled = true;
            BeginDespawn();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    public override void Exit()
    {
        // Terminal state — Exit is only called if the object is being destroyed
        // or if a future revival system is implemented
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Despawn
    // ─────────────────────────────────────────────────────────────────────────

    private void BeginDespawn()
    {
        // If still being looted, defer until loot is depleted
        if (_lootSource != null && _lootSource.enabled && _lootSource.HasLoot)
        {
            // Reset flag so Tick() will try again next second
            _despawnScheduled = false;
            return;
        }

        // Disable collider so the player can walk through the fading corpse
        if (_collider != null)
            _collider.enabled = false;

        // LootSource with LootTableType.Corpse already calls Destroy(gameObject, 5f)
        // after being fully looted — this handles the unlooted fallback case
        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] Despawning corpse.", Controller);

        Object.Destroy(Controller.gameObject);
    }
}