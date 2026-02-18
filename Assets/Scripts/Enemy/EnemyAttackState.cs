using UnityEngine;

/// <summary>
/// Attack State — Phase 3 (Stub)
/// 
/// The enemy has closed to melee range. It stops moving, faces the player,
/// and holds position. No damage, no swing animation — this is purely a
/// positional placeholder so the state machine is wired up correctly for
/// when real combat is added later.
/// 
/// The enemy rotates to keep facing the player while in this state so it
/// doesn't look broken standing sideways.
/// 
/// Transitions:
///   → EnemyChaseState        : player moves outside AttackRange
///   → EnemyReturnToPostState : leash exceeded (player dragged the enemy too far)
/// </summary>
public class EnemyAttackState : EnemyState
{
    private const float RotationSpeed = 10f;

    public EnemyAttackState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        // Stop the agent — enemy stands still in attack range
        if (Controller.Agent != null)
        {
            Controller.Agent.ResetPath();
            Controller.Agent.velocity = Vector3.zero;
        }

        Controller.SetAnimatorSpeed(0f);
        Controller.SetAnimatorAttacking(true);

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] → Attack (stub — no damage)", Controller);
    }

    public override void Tick()
    {
        // ── Leash check ──────────────────────────────────────────
        // Player could kite the enemy to the edge of its leash before entering
        // attack range — handle it here too so the enemy always returns home.
        if (Controller.IsLeashExceeded())
        {
            if (Controller.EnableDebugLogs)
                Debug.Log($"[{Controller.name}] Leash exceeded from Attack → ReturnToPost", Controller);

            Controller.ChangeState(new EnemyReturnToPostState(Controller));
            return;
        }

        // ── Player left melee range ──────────────────────────────
        if (Controller.HasPlayerLeftAttackRange())
        {
            if (Controller.EnableDebugLogs)
                Debug.Log($"[{Controller.name}] Player left attack range → Chase", Controller);

            Controller.ChangeState(new EnemyChaseState(Controller));
            return;
        }

        // ── Face the player ──────────────────────────────────────
        // Keep rotating toward the player so the enemy doesn't stand sideways.
        if (Controller.PlayerTransform != null)
        {
            Vector3 toPlayer = Controller.PlayerTransform.position - Controller.transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized);
                Controller.transform.rotation = Quaternion.Slerp(
                    Controller.transform.rotation,
                    targetRotation,
                    Time.deltaTime * RotationSpeed
                );
            }
        }
    }

    public override void Exit()
    {
        Controller.SetAnimatorAttacking(false);
    }
}