using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Chase State — Phase 2
///
/// Pursues the player using NavMeshAgent. Transitions:
///   → AttackState        : player enters melee AttackRange
///   → ReturnToPostState  : leash distance exceeded
///                          (resumes Idle, Patrol, or Wander depending on enemy setup)
///
/// The correct resume state is determined automatically from EnemyController's
/// configuration — no manual wiring needed per enemy.
/// </summary>
public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyController controller) : base(controller) { }

    // ────────────────────────────────────────────────────────────────────────
    public override void Enter()
    {
        // Store where we spotted the player so ReturnToPost has a destination
        Controller.DetectionPosition = Controller.transform.position;

        Controller.Agent.speed = Controller.ChaseSpeed;
        Controller.Agent.stoppingDistance = Controller.AttackRange * 0.9f;
        Controller.Agent.isStopped = false;

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] → Chase", Controller);
    }

    // ────────────────────────────────────────────────────────────────────────
    public override void Tick()
    {
        if (Controller.PlayerTransform == null) return;

        // ── Leash exceeded → return to post, then resume correct prior state ─
        if (Controller.IsLeashExceeded())
        {
            Controller.ChangeState(new EnemyReturnToPostState(Controller, BuildResumeState));
            return;
        }

        // ── Player in melee range → attack (stub for now) ───────────────────
        if (Controller.IsPlayerInAttackRange())
        {
            Controller.ChangeState(new EnemyAttackState(Controller));
            return;
        }

        // ── Keep chasing ────────────────────────────────────────────────────
        Controller.Agent.SetDestination(Controller.PlayerTransform.position);

        float speedNorm = Controller.Agent.velocity.magnitude / Controller.ChaseSpeed;
        Controller.SetAnimatorSpeed(speedNorm);
        Controller.RotateToVelocity();
    }

    // ────────────────────────────────────────────────────────────────────────
    public override void Exit()
    {
        Controller.Agent.ResetPath();
        Controller.Agent.velocity = Vector3.zero;
        Controller.SetAnimatorSpeed(0f);
    }

    // ── Determines which state to resume after returning to post ─────────────
    /// <summary>
    /// Builds the correct "resume" state based on this enemy's configuration.
    /// Priority mirrors EnemyController.Start():
    ///   Waypoints assigned  → PatrolState
    ///   WanderCentre set    → WanderState
    ///   Neither             → IdleState
    /// </summary>
    private EnemyState BuildResumeState()
    {
        if (Controller.HasWaypoints())
            return new EnemyPatrolState(Controller);

        if (Controller.HasWanderArea())
            return new EnemyWanderState(Controller);

        return new EnemyIdleState(Controller);
    }
}