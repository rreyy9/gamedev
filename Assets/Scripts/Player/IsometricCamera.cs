using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// IsometricCamera — Follow + Zoom + Right-Click Full Orbit
/// Unity 6000.3.8f1 | New Input System | No deprecated packages
///
/// Hold RMB and drag to orbit horizontally (yaw) and vertically (pitch)
/// around the target. Pitch is clamped so the camera can't flip underground
/// or go overhead. Zoom via scroll wheel is preserved.
/// </summary>
public class IsometricCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private float followSpeed = 5f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float currentZoom = 10f;

    [Header("Pivot")]
    [Tooltip("Horizontal mouse sensitivity. Recommended range: 0.05 – 0.5")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float yawSensitivity = 0.15f;

    [Tooltip("Vertical mouse sensitivity. Recommended range: 0.05 – 0.5")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float pitchSensitivity = 0.15f;

    [Tooltip("Lowest the camera can look (flat angle, horizon level).")]
    [Range(5f, 45f)]
    [SerializeField] private float minPitch = 15f;

    [Tooltip("Steepest downward angle the camera can reach.")]
    [Range(46f, 85f)]
    [SerializeField] private float maxPitch = 75f;

    // Runtime state
    private float _yaw = 45f;
    private float _pitch = 45f;
    private bool _isPivoting = false;
    private float _zoomInput;

    private PlayerInputActions _inputActions;

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
        _yaw = 45f;
        _pitch = 45f;
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
        _inputActions.Player.Zoom.performed += OnZoom;
        _inputActions.Player.CameraPivotHeld.performed += OnPivotStart;
        _inputActions.Player.CameraPivotHeld.canceled += OnPivotEnd;
    }

    private void OnDisable()
    {
        _inputActions.Player.Zoom.performed -= OnZoom;
        _inputActions.Player.CameraPivotHeld.performed -= OnPivotStart;
        _inputActions.Player.CameraPivotHeld.canceled -= OnPivotEnd;
        _inputActions.Player.Disable();
    }

    private void OnZoom(InputAction.CallbackContext context)
    {
        _zoomInput = context.ReadValue<float>();
    }

    private void OnPivotStart(InputAction.CallbackContext context)
    {
        _isPivoting = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnPivotEnd(InputAction.CallbackContext context)
    {
        _isPivoting = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();
        HandlePivot();
        PositionCamera();
    }

    private void HandleZoom()
    {
        if (Mathf.Abs(_zoomInput) > 0.01f)
        {
            currentZoom -= _zoomInput * zoomSpeed * 0.1f;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            _zoomInput = 0f;
        }
    }

    private void HandlePivot()
    {
        if (!_isPivoting) return;

        Vector2 delta = _inputActions.Player.CameraPivot.ReadValue<Vector2>();

        // Horizontal drag → yaw, no clamping (full 360°)
        _yaw += delta.x * yawSensitivity;

        // Vertical drag → pitch, mouse up = flatter (lower pitch value)
        // Invert Y so dragging up raises the camera, dragging down lowers it
        _pitch -= delta.y * pitchSensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    private void PositionCamera()
    {
        // Combine pitch + yaw into one orbit rotation
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPosition = target.position + rotation * new Vector3(0f, 0f, -currentZoom);

        // Smooth follow on position only (no rotation smoothing)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.LookAt(target.position);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}