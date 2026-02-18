using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player movement controller with animation support for Unity 6.
/// FIXED VERSION - Handles movement without root motion conflicts.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationSmoothTime = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    [Header("Input")]
    [SerializeField] private PlayerInputActions inputActions;

    private CharacterController characterController;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private float currentSpeed;
    private float velocityY = 0f;

    // Animation parameter IDs (cached for performance)
    private int speedHash;
    private int isGroundedHash;

    private void Awake()
    {
        // Initialize components
        characterController = GetComponent<CharacterController>();

        // Try to get Animator from this GameObject or children
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // IMPORTANT: Disable root motion to prevent double movement
        if (animator != null)
        {
            animator.applyRootMotion = false;
            Debug.Log("Root Motion disabled - script controls all movement");
        }

        // Initialize Input Actions
        inputActions = new PlayerInputActions();

        // Cache animator parameter IDs
        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            isGroundedHash = Animator.StringToHash("IsGrounded");
        }
    }

    private void OnEnable()
    {
        // Enable input and subscribe to events
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
    }

    private void OnDisable()
    {
        // Unsubscribe and disable input
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        // Read the movement input (WASD)
        moveInput = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        HandleMovement();
        UpdateAnimations();

        if (showDebugInfo)
        {
            DebugInfo();
        }
    }

    private void HandleMovement()
    {
        // Store position before movement for debugging
        Vector3 positionBefore = transform.position;

        // Convert 2D input to 3D movement for isometric view
        moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // Determine current speed based on input magnitude
        float inputMagnitude = moveInput.magnitude;
        currentSpeed = Mathf.Lerp(0f, runSpeed, inputMagnitude);

        // Apply movement using CharacterController
        if (moveDirection.magnitude >= 0.1f)
        {
            // Normalize direction for consistent speed
            Vector3 normalizedMove = moveDirection.normalized;

            // Calculate movement for this frame
            Vector3 movement = normalizedMove * currentSpeed * Time.deltaTime;

            // Move the character (THIS is the only thing moving the character)
            characterController.Move(movement);

            // Rotate character to face movement direction
            if (normalizedMove != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(normalizedMove);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        // Apply gravity
        if (characterController.isGrounded)
        {
            velocityY = -2f; // Small downward force to keep grounded
        }
        else
        {
            velocityY += Physics.gravity.y * Time.deltaTime;
        }

        Vector3 gravityMove = new Vector3(0, velocityY, 0);
        characterController.Move(gravityMove * Time.deltaTime);

        // Debug: Show how far we actually moved
        if (showDebugInfo)
        {
            Vector3 actualMovement = transform.position - positionBefore;
            if (actualMovement.magnitude > 0.01f)
            {
                Debug.DrawRay(positionBefore, actualMovement * 10f, Color.green, 0.1f);
            }
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Calculate speed value for animator (0 = idle, 0.5 = walk, 1 = run)
        float speedValue = 0f;

        if (moveInput.magnitude > 0.1f)
        {
            // Map input magnitude to animation speed
            // 0-0.5 input = walk, 0.5-1 input = run
            speedValue = Mathf.Clamp01(moveInput.magnitude);
        }

        // Smoothly update animator speed parameter
        float currentAnimSpeed = animator.GetFloat(speedHash);
        float newAnimSpeed = Mathf.Lerp(currentAnimSpeed, speedValue, animationSmoothTime);
        animator.SetFloat(speedHash, newAnimSpeed);

        // Update grounded state
        animator.SetBool(isGroundedHash, characterController.isGrounded);
    }

    private void DebugInfo()
    {
        // Debug overlay showing current state
        string debugText = $"Input: ({moveInput.x:F2}, {moveInput.y:F2})\n";
        debugText += $"Speed: {currentSpeed:F2}\n";
        debugText += $"Position: {transform.position}\n";
        debugText += $"Root Motion: {(animator != null ? animator.applyRootMotion.ToString() : "No Animator")}";

        Debug.Log(debugText);
    }

    /// <summary>
    /// Get the current movement speed (useful for other systems)
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// Check if the player is currently moving
    /// </summary>
    public bool IsMoving()
    {
        return moveInput.magnitude > 0.1f;
    }

    /// <summary>
    /// Manually set movement speed limits
    /// </summary>
    public void SetSpeedLimits(float walk, float run)
    {
        walkSpeed = walk;
        runSpeed = run;
    }
}