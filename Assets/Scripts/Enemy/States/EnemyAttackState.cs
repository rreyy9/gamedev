using UnityEngine;

/// <summary>
/// Attack State — Phase 3 Stub
///
/// Enemy has closed to melee range. No damage dealt yet — this is a placeholder
/// that stops the agent and sets IsAttacking. Transitions:
///   → ChaseState         : player moves out of attack range
///   → ReturnToPostState  : leash exceeded while attacking
///                          (resumes Idle, Patrol, or Wander depending on enemy setup)
/// </summary>
public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(EnemyController controller) : base(controller) { }

    // ────────────────────────────────────────────────────────────────────────
    public override void Enter()
    {
        Controller.Agent.isStopped = true;
        Controller.Agent.velocity = UnityEngine.Vector3.zero;

        Controller.SetAnimatorSpeed(0f);
        Controller.SetAnimatorAttacking(true);

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] → Attack (stub)", Controller);
    }

    // ────────────────────────────────────────────────────────────────────────
    public override void Tick()
    {
        // Face the player while attacking
        if (Controller.PlayerTransform != null)
        {
            Vector3 toPlayer = Controller.PlayerTransform.position - Controller.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Controller.transform.rotation = UnityEngine.Quaternion.Slerp(
                    Controller.transform.rotation,
                    UnityEngine.Quaternion.LookRotation(toPlayer),
                    UnityEngine.Time.deltaTime * 10f);
            }
        }

        // ── Leash exceeded → return to post ─────────────────────────────────
        if (Controller.IsLeashExceeded())
        {
            Controller.ChangeState(new EnemyReturnToPostState(Controller, BuildResumeState));
            return;
        }

        // ── Player escaped melee range → resume chase ────────────────────────
        if (Controller.HasPlayerLeftAttackRange())
        {
            Controller.ChangeState(new EnemyChaseState(Controller));
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    public override void Exit()
    {
        Controller.Agent.isStopped = false;
        Controller.SetAnimatorAttacking(false);
    }

    // ── Determines which state to resume after returning to post ─────────────
    private EnemyState BuildResumeState()
    {
        if (Controller.HasWaypoints())
            return new EnemyPatrolState(Controller);

        if (Controller.HasWanderArea())
            return new EnemyWanderState(Controller);

        return new EnemyIdleState(Controller);
    }
}