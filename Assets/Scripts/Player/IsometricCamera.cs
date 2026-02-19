using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Isometric camera controller that follows a target with smooth zoom.
/// Attach this to your Main Camera.
/// </summary>
public class IsometricCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f);
    [SerializeField] private float followSpeed = 5f;

    [Header("Camera Angle")]
    [SerializeField] private float isometricAngle = 45f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float currentZoom = 10f;

    // Input
    private PlayerInputActions inputActions;
    private float zoomInput;

    private void Awake()
    {
        // Create input actions instance
        inputActions = new PlayerInputActions();

        // Setup the isometric camera angle
        SetupIsometricView();
    }

    private void OnEnable()
    {
        // Enable input and subscribe to zoom
        inputActions.Player.Enable();
        inputActions.Player.Zoom.performed += OnZoom;
    }

    private void OnDisable()
    {
        // Unsubscribe and disable input
        inputActions.Player.Zoom.performed -= OnZoom;
        inputActions.Player.Disable();
    }

    private void OnZoom(InputAction.CallbackContext context)
    {
        // Read scroll wheel input
        zoomInput = context.ReadValue<float>();
    }

    private void SetupIsometricView()
    {
        // Set camera to look at isometric angle
        // 45 degrees down, 45 degrees rotated on Y axis
        transform.rotation = Quaternion.Euler(isometricAngle, 45f, 0f);
    }

    private void LateUpdate()
    {
        // Make sure we have a target
        if (target == null)
        {
            Debug.LogWarning("IsometricCamera: No target assigned! Please assign the Player in the Inspector.");
            return;
        }

        // Handle zoom first
        HandleZoom();

        // Then follow the target
        FollowTarget();
    }

    private void HandleZoom()
    {
        // Adjust zoom based on scroll wheel
        if (Mathf.Abs(zoomInput) > 0.01f)
        {
            // Scroll up = zoom in (decrease distance)
            // Scroll down = zoom out (increase distance)
            currentZoom -= zoomInput * zoomSpeed * 0.1f;

            // Clamp zoom between min and max
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            // Reset input for next frame
            zoomInput = 0f;
        }
    }

    private void FollowTarget()
    {
        // Calculate desired position based on offset and current zoom
        Vector3 direction = offset.normalized;
        Vector3 desiredPosition = target.position + (direction * currentZoom);

        // Smoothly move to desired position
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        // Always look at the target
        transform.LookAt(target.position);
    }

    /// <summary>
    /// Call this to set a new target at runtime
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}