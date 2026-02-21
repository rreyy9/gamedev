using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Scans for nearby IInteractable objects using Physics.OverlapSphere.
/// Highlights the closest valid target and dispatches the Interact input.
/// Attach to the Player GameObject.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float scanInterval = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    // Events for UI
    public event Action<string> OnInteractableFound;
    public event Action OnInteractableLost;

    // Runtime
    private IInteractable currentTarget;
    private PlayerInputActions inputActions;
    private float scanTimer;

    public IInteractable CurrentTarget => currentTarget;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.Disable();
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            ScanForInteractables();
        }
    }

    private void ScanForInteractables()
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

        if (closest != currentTarget)
        {
            // Lost previous target
            if (currentTarget != null)
            {
                currentTarget.SetHighlight(false);
                OnInteractableLost?.Invoke();
            }

            currentTarget = closest;

            // Found new target
            if (currentTarget != null)
            {
                currentTarget.SetHighlight(true);
                OnInteractableFound?.Invoke(currentTarget.InteractionPrompt);
            }
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        // If loot window is open, close it instead
        if (LootUIManager.Instance != null && LootUIManager.Instance.IsOpen)
        {
            LootUIManager.Instance.CloseLootWindow();
            return;
        }

        if (currentTarget != null && currentTarget.CanInteract)
        {
            currentTarget.Interact();
        }
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