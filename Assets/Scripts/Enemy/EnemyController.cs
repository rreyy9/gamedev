using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyController — Phase 4
/// 
/// The MonoBehaviour that owns the state machine and runs the vision cone logic.
/// Attach this to your enemy prefab root. It drives the Animator via SetAnimatorSpeed()
/// so states never touch the Animator directly — they always go through this controller.
/// 
/// SETUP CHECKLIST:
///   ✅ Attach EnemyController to enemy root GameObject
///   ✅ Assign the Animator reference in the Inspector
///   ✅ NavMeshAgent is on the same root GameObject (required for Chase/Patrol)
///   ✅ Tag the Player GameObject as "Player"
///   ✅ Tag this Enemy as "Enemy" (NOT "Player")
///   ✅ Bake a NavMesh on your terrain (AI Navigation package)
///   ✅ (Optional) Assign Waypoints to enable Patrol behaviour
/// 
/// ANIMATOR PARAMETERS REQUIRED:
///   Speed (Float)       — 0 = Idle, >0 = moving (matches existing player convention)
///   IsAttacking (Bool)  — Phase 3 stub, optional — guarded if not present in Animator Controller
/// 
/// PATROL SETUP:
///   Create empty GameObjects in the scene as waypoint markers.
///   Assign them to the Waypoints array in the Inspector.
///   Enemy will start in Patrol state instead of Idle when waypoints are present.
///   Leave Waypoints empty for a stationary enemy that only reacts when the player enters its cone.
/// </summary>
public class EnemyController : MonoBehaviour
{
    // ─────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────

    [Header("Disposition")]
    [Tooltip("Controls how this enemy reacts when the player is detected.")]
    public EnemyDisposition Disposition = EnemyDisposition.Hostile;

    [Header("Vision Cone")]
    [Tooltip("Radius of the detection check (metres).")]
    [Range(1f, 30f)]
    public float DetectionRadius = 5f;

    [Tooltip("Total width of the vision cone in degrees (e.g. 90 = 45° each side of forward).")]
    [Range(10f, 360f)]
    public float VisionAngle = 90f;

    [Header("Chase")]
    [Tooltip("How fast the enemy moves while chasing (metres/sec).")]
    [Range(1f, 10f)]
    public float ChaseSpeed = 3.5f;

    [Tooltip("Distance from spawn point at which the enemy gives up chasing and walks back to detection position.")]
    [Range(1f, 50f)]
    public float LeashDistance = 15f;

    [Tooltip("Distance at which the enemy stops chasing and enters the Attack state.")]
    [Range(0f, 5f)]
    public float AttackRange = 1.5f;

    [Header("Patrol")]
    [Tooltip("Waypoints the enemy walks between. Leave empty for a stationary enemy.")]
    public Transform[] Waypoints;

    [Tooltip("How fast the enemy moves while patrolling (metres/sec). Clamped between walk and chase speed.")]
    public float PatrolSpeed = 1.75f;

    [Tooltip("How long the enemy waits at each waypoint before moving to the next (seconds).")]
    [Range(0f, 10f)]
    public float WaypointPauseDuration = 1f;

    [Header("References")]
    [Tooltip("Animator on the character model child. Drives Speed and IsAttacking parameters.")]
    public Animator EnemyAnimator;

    [Header("Debug")]
    [Tooltip("Log state transitions and detection events to the Console.")]
    public bool EnableDebugLogs = true;

    // ─────────────────────────────────────────────────
    //  Private / Internal
    // ─────────────────────────────────────────────────

    private EnemyState _currentState;
    private Transform _playerTransform;
    private NavMeshAgent _agent;
    private Vector3 _spawnPosition;
    private float _detectionCooldownTimer = 0f;

    // Stored when the enemy first detects the player — used as the return destination
    // when the leash is exceeded so the enemy walks back to where it gave chase from.
    [HideInInspector] public Vector3 DetectionPosition;

    // Cached Animator parameter hashes — same pattern as PlayerMovement
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    // ─────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────

    private void OnValidate()
    {
        // Walk threshold maps to ~2f, run to ChaseSpeed — keep patrol inside that range
        const float walkSpeed = 2f;
        PatrolSpeed = Mathf.Clamp(PatrolSpeed, walkSpeed, ChaseSpeed);
    }

    private void Awake()
    {
        // Record spawn position for leash calculations
        _spawnPosition = transform.position;

        // Cache NavMeshAgent
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError($"[{name}] EnemyController: No NavMeshAgent found. " +
                           "Add a NavMeshAgent component to the enemy root.", this);
        }
        else
        {
            _agent.autoBraking = false;
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            _agent.stoppingDistance = AttackRange * 0.9f;
        }

        // Find the player — exclude self and other enemies
        GameObject[] candidates = GameObject.FindGameObjectsWithTag("Player");
        foreach (var candidate in candidates)
        {
            if (candidate == gameObject) continue;
            if (candidate.GetComponent<EnemyController>() != null) continue;
            _playerTransform = candidate.transform;
            break;
        }

        if (_playerTransform == null)
        {
            if (candidates.Length == 0)
                Debug.LogError($"[{name}] EnemyController: No GameObject tagged 'Player' found.", this);
            else
                Debug.LogError($"[{name}] EnemyController: Player-tagged objects found but all excluded. " +
                               "Is this enemy accidentally tagged 'Player'?", this);
        }

        if (EnemyAnimator == null)
            Debug.LogWarning($"[{name}] EnemyController: EnemyAnimator not assigned in Inspector.", this);
    }

    private void Start()
    {
        // Enter Patrol if waypoints are assigned, otherwise stand idle
        if (HasWaypoints())
            ChangeState(new EnemyPatrolState(this));
        else
            ChangeState(new EnemyIdleState(this));
    }

    private void Update()
    {
        if (_detectionCooldownTimer > 0f)
            _detectionCooldownTimer -= Time.deltaTime;

        _currentState?.Tick();
    }

    // ─────────────────────────────────────────────────
    //  State Machine
    // ─────────────────────────────────────────────────

    public void ChangeState(EnemyState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    // ─────────────────────────────────────────────────
    //  Detection Helpers
    // ─────────────────────────────────────────────────

    /// <summary>Returns true when the player is inside the vision cone (distance + angle).</summary>
    public bool IsPlayerInVisionCone()
    {
        if (_playerTransform == null) return false;

        Vector3 toPlayer = _playerTransform.position - transform.position;
        if (toPlayer.magnitude > DetectionRadius) return false;

        Vector3 toPlayerFlat = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;
        Vector3 enemyForwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        float angle = Vector3.Angle(enemyForwardFlat, toPlayerFlat);

        return angle <= VisionAngle * 0.5f;
    }

    /// <summary>Returns true when the player is within melee AttackRange.</summary>
    public bool IsPlayerInAttackRange()
    {
        if (_playerTransform == null) return false;
        return Vector3.Distance(transform.position, _playerTransform.position) <= AttackRange;
    }

    /// <summary>
    /// Returns true when the player has moved outside melee AttackRange.
    /// Uses a small buffer to prevent rapid Attack↔Chase flickering at the boundary.
    /// </summary>
    public bool HasPlayerLeftAttackRange()
    {
        if (_playerTransform == null) return false;
        return Vector3.Distance(transform.position, _playerTransform.position) > AttackRange * 1.2f;
    }

    /// <summary>Returns true when the enemy has wandered further than LeashDistance from spawn.</summary>
    public bool IsLeashExceeded()
    {
        return Vector3.Distance(transform.position, _spawnPosition) > LeashDistance;
    }

    /// <summary>Returns true when detection is not on cooldown.</summary>
    public bool CanDetectPlayer()
    {
        return _detectionCooldownTimer <= 0f;
    }

    /// <summary>Suppresses vision cone detection for the given duration (seconds).</summary>
    public void StartDetectionCooldown(float duration)
    {
        _detectionCooldownTimer = duration;
    }

    /// <summary>Returns true if this enemy has at least one waypoint assigned.</summary>
    public bool HasWaypoints()
    {
        return Waypoints != null && Waypoints.Length > 0;
    }

    // ─────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────

    public Transform PlayerTransform => _playerTransform;
    public NavMeshAgent Agent => _agent;
    public Vector3 SpawnPosition => _spawnPosition;

    // ─────────────────────────────────────────────────
    //  Animator Helpers
    // ─────────────────────────────────────────────────

    /// <summary>Sets the Speed float on the Animator (0 = Idle, >0 = moving).</summary>
    public void SetAnimatorSpeed(float speed)
    {
        if (EnemyAnimator == null) return;
        EnemyAnimator.SetFloat(SpeedHash, speed);
    }

    /// <summary>Sets the IsAttacking bool on the Animator if the parameter exists.</summary>
    public void SetAnimatorAttacking(bool isAttacking)
    {
        if (EnemyAnimator == null) return;
        foreach (var param in EnemyAnimator.parameters)
        {
            if (param.nameHash == IsAttackingHash)
            {
                EnemyAnimator.SetBool(IsAttackingHash, isAttacking);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────
    //  Scene Gizmos
    // ─────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Vision cone colour by disposition
        switch (Disposition)
        {
            case EnemyDisposition.Hostile: Gizmos.color = new Color(1f, 0f, 0f, 0.35f); break;
            case EnemyDisposition.Neutral: Gizmos.color = new Color(1f, 0.92f, 0f, 0.35f); break;
            case EnemyDisposition.Passive: Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.35f); break;
        }

        Gizmos.DrawWireSphere(transform.position, DetectionRadius);

        float halfAngle = VisionAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0f, halfAngle, 0f) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDir * DetectionRadius);
        Gizmos.DrawRay(transform.position, rightDir * DetectionRadius);

        Color solid = Gizmos.color; solid.a = 1f; Gizmos.color = solid;
        Gizmos.DrawRay(transform.position, transform.forward * DetectionRadius);

        // Attack range — orange
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // Leash range — blue
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Vector3 leashOrigin = Application.isPlaying ? _spawnPosition : transform.position;
        Gizmos.DrawWireSphere(leashOrigin, LeashDistance);

        // Patrol waypoints — green lines connecting each point
        if (Waypoints != null && Waypoints.Length > 1)
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
            for (int i = 0; i < Waypoints.Length; i++)
            {
                if (Waypoints[i] == null) continue;
                int next = (i + 1) % Waypoints.Length;
                if (Waypoints[next] == null) continue;
                Gizmos.DrawLine(Waypoints[i].position, Waypoints[next].position);
                Gizmos.DrawWireSphere(Waypoints[i].position, 0.2f);
            }
        }
    }
}