using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Phase 5 – Wander State
/// Free-roaming behaviour for animals and ambient wildlife.
/// Picks random destinations biased toward a defined centre point,
/// waits for a randomised pause at each destination, then moves again.
///
/// Transitions:
///   → EnemyChaseState  : Hostile disposition + player enters vision cone
///   (No leash logic – the territory radius replaces leash)
/// </summary>
public class EnemyWanderState : EnemyState
{
    // ── Runtime ─────────────────────────────────────────────────────────────
    private Vector3 _centrePosition;
    private bool _isWaiting;
    private float _pauseTimer;
    private float _idleLookTimer;
    private float _idleLookDuration = 1.5f;
    private Quaternion _idleTargetRotation;

    private const int MaxPickAttempts = 5;
    private const float SampleRadius = 1.5f;   // NavMesh.SamplePosition snap radius

    // ── Constructor ─────────────────────────────────────────────────────────
    public EnemyWanderState(EnemyController controller) : base(controller) { }

    // ────────────────────────────────────────────────────────────────────────
    public override void Enter()
    {
        // Determine centre – fall back to spawn position when null
        _centrePosition = Controller.WanderCentre != null
            ? Controller.WanderCentre.position
            : Controller.SpawnPosition;

        Controller.Agent.speed = Controller.PatrolSpeed;
        Controller.Agent.stoppingDistance = Controller.ArrivalThreshold;
        Controller.Agent.isStopped = false;

        _isWaiting = false;
        PickNewDestination();
    }

    // ────────────────────────────────────────────────────────────────────────
    public override void Tick()
    {
        // ── Vision cone check (same pattern as patrol / idle) ───────────────
        if (Controller.Disposition == EnemyDisposition.Hostile &&
            Controller.CanSeePlayer())
        {
            Controller.ChangeState(new EnemyChaseState(Controller));
            return;
        }

        if (_isWaiting)
        {
            TickWait();
        }
        else
        {
            TickMove();
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

    // ── Private helpers ──────────────────────────────────────────────────────

    private void TickMove()
    {
        // Normalise against ChaseSpeed so walk blend sits at ~0.5 threshold
        float speedNorm = Controller.Agent.velocity.magnitude / Controller.ChaseSpeed;
        Controller.SetAnimatorSpeed(speedNorm);
        Controller.RotateToVelocity();

        // Check arrival
        bool arrived = !Controller.Agent.pathPending &&
                       Controller.Agent.remainingDistance <= Controller.ArrivalThreshold;

        if (arrived)
        {
            StartWaitingPause();
        }
    }

    private void TickWait()
    {
        Controller.SetAnimatorSpeed(0f);
        IdleLookAround();

        _pauseTimer -= Time.deltaTime;
        if (_pauseTimer <= 0f)
        {
            _isWaiting = false;
            PickNewDestination();
        }
    }

    /// <summary>
    /// Slowly rotates the enemy toward a random direction while idling,
    /// giving a natural "looking around" feel.
    /// </summary>
    private void IdleLookAround()
    {
        _idleLookTimer -= Time.deltaTime;
        if (_idleLookTimer <= 0f)
        {
            // Pick a new random direction to face
            float angle = Random.Range(0f, 360f);
            _idleTargetRotation = Quaternion.Euler(0f, angle, 0f);
            _idleLookTimer = _idleLookDuration;
        }

        Controller.transform.rotation = Quaternion.Slerp(
            Controller.transform.rotation,
            _idleTargetRotation,
            Time.deltaTime * 2f);
    }

    private void StartWaitingPause()
    {
        _isWaiting = true;
        _pauseTimer = Random.Range(Controller.WanderPauseMin, Controller.WanderPauseMax);

        // Seed idle look timer so the first direction change happens quickly
        _idleLookTimer = Random.Range(0f, _idleLookDuration);
        _idleTargetRotation = Controller.transform.rotation;

        Controller.Agent.ResetPath();
        Controller.Agent.velocity = Vector3.zero;
    }

    /// <summary>
    /// Picks a random destination biased toward the wander centre,
    /// snapped to a valid NavMesh surface.
    /// </summary>
    private void PickNewDestination()
    {
        for (int attempt = 0; attempt < MaxPickAttempts; attempt++)
        {
            Vector3 candidate = RandomPointInTerritory();

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas))
            {
                Controller.Agent.SetDestination(hit.position);
                Controller.Agent.isStopped = false;
                return;
            }
        }

        // All attempts failed – idle briefly then retry on next cycle
        Debug.LogWarning($"[EnemyWanderState] {Controller.name}: Could not find valid NavMesh point " +
                         $"after {MaxPickAttempts} attempts. Idling briefly.");
        StartWaitingPause();
    }

    /// <summary>
    /// Returns a random world position within the wander territory,
    /// biased toward the centre using two multiplied randoms.
    /// </summary>
    private Vector3 RandomPointInTerritory()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        // Multiply two 0–1 ranges together → distribution weighted toward 0 (centre)
        float distance = Controller.WanderRadius *
                         Random.Range(0f, 1f) *
                         Random.Range(0f, 1f);

        float x = _centrePosition.x + Mathf.Cos(angle) * distance;
        float z = _centrePosition.z + Mathf.Sin(angle) * distance;

        return new Vector3(x, _centrePosition.y, z);
    }
}