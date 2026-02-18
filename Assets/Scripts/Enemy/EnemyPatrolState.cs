using UnityEngine;

/// <summary>
/// Patrol State — Phase 4
/// 
/// The enemy walks between the waypoints assigned in EnemyController's Inspector array.
/// The vision cone scan runs every frame while patrolling — if the player is detected
/// the enemy immediately transitions to Chase.
/// 
/// Waypoint behaviour:
///   - Loops through the waypoints array in order, wrapping back to index 0
///   - Waits at each waypoint for WaypointPauseDuration seconds before moving on
///   - The enemy faces its movement direction while walking (same as Chase)
///   - While paused at a waypoint the enemy faces the direction it arrived from
/// 
/// Transitions:
///   → EnemyChaseState : player detected inside vision cone
///   → EnemyIdleState  : no waypoints assigned (safety fallback)
/// </summary>
public class EnemyPatrolState : EnemyState
{
    private const float ArrivalThreshold = 0.4f;
    private const float RotationSpeed = 8f;

    private int _currentWaypointIndex = 0;
    private float _pauseTimer = 0f;
    private bool _isWaiting = false;

    public EnemyPatrolState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        // Safety — if there are no waypoints somehow, fall back to Idle
        if (!Controller.HasWaypoints())
        {
            Controller.ChangeState(new EnemyIdleState(Controller));
            return;
        }

        Controller.Agent.speed = Controller.PatrolSpeed;
        Controller.Agent.stoppingDistance = ArrivalThreshold;

        // Head to the first waypoint immediately
        MoveToCurrentWaypoint();

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] → Patrol ({Controller.Waypoints.Length} waypoints)", Controller);
    }

    public override void Tick()
    {
        // ── Vision cone scan ─────────────────────────────────────
        // Check every frame regardless of whether we're moving or waiting.
        // The enemy should react to the player even while paused at a waypoint.
        if (Controller.CanDetectPlayer() && Controller.IsPlayerInVisionCone())
        {
            if (Controller.Disposition == EnemyDisposition.Hostile)
            {
                Controller.DetectionPosition = Controller.transform.position;

                if (Controller.EnableDebugLogs)
                    Debug.Log($"[{Controller.name}] Patrol → player detected → Chase", Controller);

                Controller.ChangeState(new EnemyChaseState(Controller));
                return;
            }
        }

        // ── Waiting at waypoint ──────────────────────────────────
        if (_isWaiting)
        {
            _pauseTimer -= Time.deltaTime;

            // Keep Speed at 0 during the pause
            Controller.SetAnimatorSpeed(0f);

            if (_pauseTimer <= 0f)
            {
                _isWaiting = false;
                AdvanceWaypoint();
                MoveToCurrentWaypoint();
            }

            return;
        }

        // ── Moving to waypoint ───────────────────────────────────

        // Sync animation
        float speedNormalised = Controller.Agent.velocity.magnitude / Controller.ChaseSpeed;
        Controller.SetAnimatorSpeed(speedNormalised);

        // Rotate to face movement direction
        Vector3 velocity = Controller.Agent.velocity;
        if (velocity.sqrMagnitude > 0.01f)
        {
            velocity.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
            Controller.transform.rotation = Quaternion.Slerp(
                Controller.transform.rotation,
                targetRotation,
                Time.deltaTime * RotationSpeed
            );
        }

        // Check if arrived at waypoint
        if (!Controller.Agent.pathPending &&
            Controller.Agent.remainingDistance <= ArrivalThreshold)
        {
            // Start the pause before moving to the next point
            _isWaiting = true;
            _pauseTimer = Controller.WaypointPauseDuration;
            Controller.SetAnimatorSpeed(0f);
        }
    }

    public override void Exit()
    {
        Controller.Agent.ResetPath();
        Controller.Agent.velocity = Vector3.zero;
        Controller.SetAnimatorSpeed(0f);

        // Reset stopping distance back to attack range for Chase/Attack states
        Controller.Agent.stoppingDistance = Controller.AttackRange * 0.9f;
    }

    // ─────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────

    private void MoveToCurrentWaypoint()
    {
        if (Controller.Waypoints[_currentWaypointIndex] == null)
        {
            // Skip null waypoints gracefully
            AdvanceWaypoint();
            MoveToCurrentWaypoint();
            return;
        }

        Controller.Agent.SetDestination(Controller.Waypoints[_currentWaypointIndex].position);
    }

    private void AdvanceWaypoint()
    {
        _currentWaypointIndex = (_currentWaypointIndex + 1) % Controller.Waypoints.Length;
    }
}