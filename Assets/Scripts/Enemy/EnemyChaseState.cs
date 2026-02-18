using UnityEngine;

/// <summary>
/// Chase State — Phase 2 (updated Phase 3)
/// 
/// The enemy uses NavMeshAgent to actively pursue the player.
/// The character model rotates smoothly to face its movement direction
/// using the same pattern as PlayerMovement (velocity-based rotation).
/// 
/// Transitions:
///   → EnemyAttackState       : player enters AttackRange
///   → EnemyReturnToPostState : leash distance exceeded
///   → EnemyIdleState         : NavMesh path becomes invalid
/// </summary>
public class EnemyChaseState : EnemyState
{
    private const float RotationSpeed = 10f;
    private const float DestinationUpdateInterval = 0.1f;
    private float _destinationTimer;

    public EnemyChaseState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        if (Controller.Agent == null) return;

        Controller.Agent.speed = Controller.ChaseSpeed;
        Controller.Agent.SetDestination(Controller.PlayerTransform.position);
        Controller.SetAnimatorSpeed(1f);

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] → Chase", Controller);
    }

    public override void Tick()
    {
        if (Controller.Agent == null) return;

        // ── Leash check ──────────────────────────────────────────
        if (Controller.IsLeashExceeded())
        {
            if (Controller.EnableDebugLogs)
                Debug.Log($"[{Controller.name}] Leash exceeded → ReturnToPost", Controller);

            Controller.ChangeState(new EnemyReturnToPostState(Controller));
            return;
        }

        // ── Attack range check ───────────────────────────────────
        if (Controller.IsPlayerInAttackRange())
        {
            if (Controller.EnableDebugLogs)
                Debug.Log($"[{Controller.name}] Player in attack range → Attack", Controller);

            Controller.ChangeState(new EnemyAttackState(Controller));
            return;
        }

        // ── Path validity check ──────────────────────────────────
        if (Controller.Agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            if (Controller.EnableDebugLogs)
                Debug.Log($"[{Controller.name}] Path invalid → Idle", Controller);

            Controller.ChangeState(new EnemyIdleState(Controller));
            return;
        }

        // ── Update destination on a timer ────────────────────────
        _destinationTimer += Time.deltaTime;
        if (_destinationTimer >= DestinationUpdateInterval)
        {
            _destinationTimer = 0f;
            Controller.Agent.SetDestination(Controller.PlayerTransform.position);
        }

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

        // ── Animator sync ────────────────────────────────────────
        float speedNormalised = Controller.Agent.velocity.magnitude / Controller.ChaseSpeed;
        Controller.SetAnimatorSpeed(speedNormalised);
    }

    public override void Exit()
    {
        if (Controller.Agent == null) return;

        Controller.Agent.ResetPath();
        Controller.Agent.velocity = Vector3.zero;
        Controller.SetAnimatorSpeed(0f);
    }
}