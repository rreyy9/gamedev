using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGatheringController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GatheringCastBarUI castBar;

    // How far the player must move (in Unity units) to cancel — small value catches any WASD input
    [SerializeField] private float moveCancelThreshold = 0.1f;

    private static readonly int IsMiningHash = Animator.StringToHash("IsMining");

    private PlayerInputActions _inputActions;
    private Vector3 _miningStartPosition;
    private bool _cancelRequested;
    private bool _isMiningActive;

    // MiningNode reads these each frame during the cast
    public bool IsCancelRequested => _cancelRequested;
    public bool HasPlayerMoved =>
        _isMiningActive &&
        Vector3.Distance(transform.position, _miningStartPosition) > moveCancelThreshold;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
        _inputActions.Player.Cancel.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        _inputActions.Player.Cancel.performed -= OnCancelPerformed;
        _inputActions.Player.Disable();
    }

    // ─────────────────────────────────────────────
    //  Input Callback
    // ─────────────────────────────────────────────

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (_isMiningActive)
        {
            _cancelRequested = true;
            Debug.Log("[PGC] Cancel key pressed — cancelling mining.");
        }
    }

    // ─────────────────────────────────────────────
    //  Public API (called by MiningNode)
    // ─────────────────────────────────────────────

    public void StartMining(float duration)
    {
        if (animator == null)
        {
            Debug.LogError("[PGC] Animator is NULL — drag PlayerModel into the Animator field on PlayerGatheringController!");
            return;
        }

        _miningStartPosition = transform.position;
        _cancelRequested = false;
        _isMiningActive = true;

        animator.SetBool(IsMiningHash, true);
        castBar?.Show(duration);

        Debug.Log($"[PGC] StartMining — duration={duration}, startPos={_miningStartPosition}");
    }

    public void StopMining()
    {
        _isMiningActive = false;
        _cancelRequested = false;

        if (animator != null)
            animator.SetBool(IsMiningHash, false);

        castBar?.Hide();
        Debug.Log("[PGC] StopMining called.");
    }
}