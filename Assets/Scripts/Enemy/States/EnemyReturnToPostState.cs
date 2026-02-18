using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// Return To Post State — Phase 2+
///
/// Walks the enemy back toward its detection position (where it first spotted
/// the player) then resumes whatever state it came from — Idle, Patrol, or Wander.
///
/// Usage:
///   // From Chase — resume Idle
///   Controller.ChangeState(new EnemyReturnToPostState(Controller,
///       () => new EnemyIdleState(Controller)));
///
///   // From Chase — resume Patrol
///   Controller.ChangeState(new EnemyReturnToPostState(Controller,
///       () => new EnemyPatrolState(Controller)));
///
///   // From Chase — resume Wander
///   Controller.ChangeState(new EnemyReturnToPostState(Controller,
///       () => new EnemyWanderState(Controller)));
///
/// The caller (typically EnemyChaseState) passes a factory lambda so the
/// correct state is re-created fresh when the enemy arrives back at post.
/// </summary>
public class EnemyReturnToPostState : EnemyState
{
    // ── Factory for the state to resume when we arrive ───────────────────────
    private readonly Func<EnemyState> _resumeStateFactory;

    // ── Cached destination ───────────────────────────────────────────────────
    private Vector3 _destination;

    // ── Re-aggro guard ───────────────────────────────────────────────────────
    private const float ReAggroCooldown = 3f;
    private const float ArrivalBuffer = 0.5f;   // extra tolerance on top of ArrivalThreshold

    // ── Constructor ──────────────────────────────────────────────────────────
    /// <param name="controller">The owning EnemyController.</param>
    /// <param name="resumeStateFactory">
    ///     Lambda that creates the state to return to on arrival.
    ///     E.g. <c>() => new EnemyPatrolState(Controller)</c>
    /// </param>
    public EnemyReturnToPostState(EnemyController controller, Func<EnemyState> resumeStateFactory)
        : base(controller)
    {
        _resumeStateFactory = resumeStateFactory;
    }

    // ────────────────────────────────────────────────────────────────────────
    public override void Enter()
    {
        // Start re-aggro cooldown so the enemy won't immediately detect the player again
        Controller.StartDetectionCooldown(ReAggroCooldown);

        // Walk back to where the enemy first spotted the player
        _destination = Controller.DetectionPosition;

        Controller.Agent.speed = Controller.PatrolSpeed;
        Controller.Agent.stoppingDistance = Controller.ArrivalThreshold;
        Controller.Agent.isStopped = false;
        Controller.Agent.SetDestination(_destination);

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] → ReturnToPost  dest={_destination}", Controller);
    }

    // ────────────────────────────────────────────────────────────────────────
    public override void Tick()
    {
        float speedNorm = Controller.Agent.velocity.magnitude / Controller.ChaseSpeed;
        Controller.SetAnimatorSpeed(speedNorm);
        Controller.RotateToVelocity();

        // Check arrival
        bool arrived = !Controller.Agent.pathPending &&
                       Controller.Agent.remainingDistance <= Controller.ArrivalThreshold + ArrivalBuffer;

        if (arrived)
        {
            // Resume whichever state we came from (Idle / Patrol / Wander)
            Controller.ChangeState(_resumeStateFactory());
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    public override void Exit()
    {
        Controller.Agent.ResetPath();
        Controller.Agent.velocity = Vector3.zero;
        Controller.Agent.stoppingDistance = Controller.AttackRange * 0.9f;
        Controller.SetAnimatorSpeed(0f);
    }
}