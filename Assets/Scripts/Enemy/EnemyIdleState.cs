using UnityEngine;

/// <summary>
/// Idle State — Phase 2
/// 
/// The default entry state. Enemy stands still, runs the vision cone scan each frame.
/// Guards detection behind CanDetectPlayer() so the re-aggro cooldown from
/// EnemyReturnToPostState is respected.
/// 
/// Transitions:
///   Hostile  → player detected inside cone → EnemyChaseState
///   Neutral  → player detected inside cone → Alert stub (future)
///   Passive  → never reacts
/// </summary>
public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        // Stop the NavMeshAgent if it was moving (e.g. returning from Chase or ReturnToPost)
        if (Controller.Agent != null)
        {
            Controller.Agent.ResetPath();
            Controller.Agent.velocity = Vector3.zero;
        }

        Controller.SetAnimatorSpeed(0f);

        if (Controller.EnableDebugLogs)
            Debug.Log($"[{Controller.name}] → Idle", Controller);
    }

    public override void Tick()
    {
        // Respect the re-aggro cooldown set by EnemyReturnToPostState.
        // Without this guard, the enemy would instantly re-detect the player
        // as soon as it arrives back at DetectionPosition and enters Idle.
        if (!Controller.CanDetectPlayer()) return;

        if (!Controller.IsPlayerInVisionCone()) return;

        switch (Controller.Disposition)
        {
            case EnemyDisposition.Hostile:
                // Save where the enemy is standing right now — this is the position
                // EnemyReturnToPostState will walk back to if the leash is exceeded.
                Controller.DetectionPosition = Controller.transform.position;

                Controller.ChangeState(new EnemyChaseState(Controller));
                break;

            case EnemyDisposition.Neutral:
                // Future: Controller.ChangeState(new EnemyAlertState(Controller));
                if (Controller.EnableDebugLogs)
                    Debug.Log($"[{Controller.name}] Neutral → player detected (Alert state coming later)", Controller);
                break;

            case EnemyDisposition.Passive:
                break;
        }
    }

    public override void Exit()
    {
        // Nothing to clean up
    }
}