using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// WoW-style right-click interaction system.
/// Raycasts from the mouse cursor on right-click.
/// If the ray hits an IInteractable, it interacts and suppresses camera orbit.
/// If not, camera orbit proceeds as normal.
/// Attach to the Player GameObject.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float scanInterval = 0.1f;

    [Header("Raycast Settings")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float raycastMaxDistance = 50f;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    // Public flag — IsometricCamera reads this to suppress orbit
    public bool IsInteractingThisFrame { get; private set; }

    // Runtime
    private IInteractable currentHighlightTarget;
    private PlayerInputActions inputActions;
    private float scanTimer;

    public IInteractable CurrentTarget => currentHighlightTarget;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        // Auto-find camera if not assigned
        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void OnEnable()
    {
        inputActions.Player.RightClickInteract.performed += OnRightClickPerformed;
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.RightClickInteract.performed -= OnRightClickPerformed;
        inputActions.Player.Disable();
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            ScanForNearbyHighlight();
        }
    }

    private void LateUpdate()
    {
        // Reset after camera's LateUpdate has had a chance to read it.
        // Script execution order ensures IsometricCamera LateUpdate runs first
        // (set this in Project Settings if needed).
        IsInteractingThisFrame = false;
    }

    /// <summary>
    /// Scans nearby objects for highlight purposes only (shows the prompt when you're close).
    /// Actual interaction is triggered by right-click raycast.
    /// </summary>
    private void ScanForNearbyHighlight()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);

        IInteractable closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable == null || !interactable.CanInteract) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        if (closest != currentHighlightTarget)
        {
            if (currentHighlightTarget != null)
            {
                currentHighlightTarget.SetHighlight(false);
            }

            currentHighlightTarget = closest;

            if (currentHighlightTarget != null)
            {
                currentHighlightTarget.SetHighlight(true);
            }
        }
    }

    /// <summary>
    /// Called when right mouse button is pressed.
    /// Raycasts from cursor — if it hits an interactable, interact and block camera orbit.
    /// </summary>
    private void OnRightClickPerformed(InputAction.CallbackContext context)
    {
        // If loot window is open, close it first
        if (LootUIManager.Instance != null && LootUIManager.Instance.IsOpen)
        {
            LootUIManager.Instance.CloseLootWindow();
            IsInteractingThisFrame = true; // Still suppress camera
            return;
        }

        // Raycast from mouse cursor into the world
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = gameCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, interactableLayer))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null && interactable.CanInteract)
            {
                // Use hit.distance (ray from camera) as a sanity check, but primarily
                // trust the player-to-hit-point distance so pivot offsets don't skew it
                float dist = Vector3.Distance(transform.position, hit.point);
                if (dist <= interactRange)
                {
                    interactable.Interact();
                    IsInteractingThisFrame = true;
                    return;
                }
                else
                {
                    Debug.Log($"[PlayerInteraction] {interactable.InteractionPrompt} — too far away ({dist:F1}m, max {interactRange}m).");
                }
            }
        }

        // Nothing interactable was clicked — camera orbit will proceed normally
    }

    private void OnDrawGizmosSelected()
    {
        if (showGizmo)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }
}