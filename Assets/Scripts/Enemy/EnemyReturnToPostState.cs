using UnityEngine;

/// <summary>
/// Return To Post State — Phase 2 addition
/// 
/// Triggered when the chase leash is exceeded. The enemy walks back to the position
/// it was standing when it first detected the player (DetectionPosition), then
/// enters Idle with a short re-aggro cooldown so it doesn't instantly re-chase.
/// 
/// This breaks the rapid Idle ↔ Chase loop that occurs when:
///   1. Enemy gives up and goes Idle
///   2. Player is still visible from that position
///   3. Enemy immediately re-enters Chase
///   4. Leash fires again → repeat forever → jitter on the spot
/// 
/// Transitions:
///   → EnemyIdleState : arrived at DetectionPosition (triggers 2s detection cooldown)
/// </summary>
public class EnemyReturnToPostState : EnemyState
{
    // How close the enemy needs to be to DetectionPosition to count as "arrived"
    private const float ArrivalThreshold = 0.5f;

    // Walk back slightly slower than the chase speed — feels more natural
    private const float ReturnSpeedMultiplier = 0.75f;

    // How long after arriving before the enemy can detect the player again.
    // Gives the player a moment to back off before getting re-aggroed.
    private const float ReAggroCooldown = 2f;

    // How quickly the enemy rotates to face its movement direction
    private const float RotationSpeed = 8f;

    public EnemyReturnToPostState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        if (Controller.Agent == null) return;

        // Walk back at a fraction of chase speed
        Controller.Agent.speed = Controller.ChaseSpeed * ReturnSpeedMultiplier;

        // Head toward where the enemy was standing when it first spotted the player.
        // This was saved by EnemyIdleState just before it transitioned to Chase.
        Controller.Agent.SetDestination(Controller.DetectionPosition);

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] → ReturnToPost (heading to detection position {Controller.DetectionPosition})", Controller);
    }

    public override void Tick()
    {
        if (Controller.Agent == null) return;

        // ── Animator sync ────────────────────────────────────────
        // Normalise against the return speed so the blend tree matches correctly
        float returnSpeed = Controller.ChaseSpeed * ReturnSpeedMultiplier;
        float speedNormalised = Controller.Agent.velocity.magnitude / returnSpeed;
        Controller.SetAnimatorSpeed(speedNormalised);

        // ── Rotation ─────────────────────────────────────────────
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

        // ── Arrival check ─────────────────────────────────────────
        // pathPending guard prevents a false positive on the first frame before
        // the NavMesh has finished calculating the path.
        if (!Controller.Agent.pathPending &&
            Controller.Agent.remainingDistance <= ArrivalThreshold)
        {
            // Return to Patrol if this enemy has waypoints, otherwise stand Idle
            if (Controller.HasWaypoints())
                Controller.ChangeState(new EnemyPatrolState(Controller));
            else
                Controller.ChangeState(new EnemyIdleState(Controller));
        }
    }

    public override void Exit()
    {
        if (Controller.Agent == null) return;

        // Clean up movement
        Controller.Agent.ResetPath();
        Controller.Agent.velocity = Vector3.zero;
        Controller.SetAnimatorSpeed(0f);

        // Start the cooldown here in Exit() so it fires regardless of how we leave
        // this state — keeps EnemyIdleState's Tick() from immediately re-detecting.
        Controller.StartDetectionCooldown(ReAggroCooldown);

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] ReturnToPost complete — detection suppressed for {ReAggroCooldown}s", Controller);
    }
}