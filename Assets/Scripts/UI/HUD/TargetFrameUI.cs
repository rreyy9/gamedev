using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// TargetFrameUI — WoW-Style Enemy Target Frame (HUD)
/// Unity 6000.3.8f1 | New Input System
///
/// Clicking on an enemy (left mouse button) sets that enemy as the current
/// target and populates this HUD panel with their name, health bar, and HP
/// numbers — exactly like WoW's target unit frame.
///
/// Clicking empty space or a non-enemy clears the target.
///
/// HIERARCHY SETUP:
///
///   TargetFrame (RectTransform — anchor: bottom-left, next to player frame)
///   ├── TargetNameText (TMP_Text)
///   ├── HealthBarBackground (Image — dark track)
///   │   └── HealthBarFill (Image — Filled, Horizontal) ← assign this
///   └── HPText (TMP_Text — "85 / 100")
///
///   Attach this script to the TargetFrame root.
///   Set the TargetFrame panel INACTIVE by default — this script shows/hides it.
///
/// SETUP CHECKLIST:
///   ☑ BarFill: Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left
///   ☑ TargetFrame starts INACTIVE in the Inspector (script enables it on target)
///   ☑ Main Camera tagged "MainCamera"
///   ☑ Enemy GameObjects on an "Enemy" layer (set layerMask in Inspector)
/// </summary>
public class TargetFrameUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI References")]
    [Tooltip("TMP_Text showing the targeted enemy's name.")]
    [SerializeField] private TMP_Text targetNameText;

    [Tooltip("Image fill for the enemy health bar. Type=Filled, Method=Horizontal.")]
    [SerializeField] private Image barFill;

    [Tooltip("(Optional) HP numbers overlaid on or below the bar.")]
    [SerializeField] private TMP_Text hpText;

    [Header("Bar Colours")]
    [SerializeField] private Color hostileColor = new Color(0.78f, 0.10f, 0.10f);
    [SerializeField] private Color neutralColor = new Color(0.90f, 0.80f, 0.10f);
    [SerializeField] private Color friendlyColor = new Color(0.10f, 0.78f, 0.20f);

    [Header("Targeting")]
    [Tooltip("Layer(s) that can be targeted. Set to your Enemy layer.")]
    [SerializeField] private LayerMask targetableLayers;

    [Tooltip("Max raycast distance for click-to-target.")]
    [SerializeField] private float maxTargetDistance = 50f;

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    private HealthComponent _currentTarget;
    private EnemyController _currentController;
    private EnemyHealthBarUI _currentNameplate;
    private Camera _mainCamera;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

private void Awake()
    {
        _mainCamera = Camera.main;
        // Hide the visual panel but keep this component alive for input detection.
        // We cannot call gameObject.SetActive(false) here because Update() would
        // stop running and clicks would never be detected. Instead we hide all
        // child visuals via a CanvasGroup, or simply ensure the panel starts
        // inactive in the scene and we re-enable it immediately so Update runs.
        HidePanel();
    }

    private void Update()
    {
        // ── Left-click to target ──────────────────────────────────────────────
        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleClickTarget();
    }

private void OnDisable()
    {
        // Intentionally empty — we no longer disable this GameObject,
        // so OnDisable will not fire during normal gameplay.
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Targeting
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleClickTarget()
    {
        // Don't retarget if clicking on UI (inventory, etc.)
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, maxTargetDistance, targetableLayers))
        {
            var health = hit.collider.GetComponentInParent<HealthComponent>();
            var controller = hit.collider.GetComponentInParent<EnemyController>();

            if (health != null && !health.IsDead)
                SetTarget(health, controller);
            else
                ClearTarget();
        }
        else
        {
            ClearTarget();
        }
    }

    private void SetTarget(HealthComponent health, EnemyController controller)
    {
        // Unsubscribe from old target
        if (_currentTarget != null)
        {
            _currentTarget.OnHealthChanged -= Refresh;
            _currentTarget.OnDied -= OnTargetDied;
        }

        // Deselect old nameplate highlight
        if (_currentNameplate != null)
            _currentNameplate.SetTargeted(false);

        // Assign new target
        _currentTarget = health;
        _currentController = controller;

        // Subscribe to new target
        _currentTarget.OnHealthChanged += Refresh;
        _currentTarget.OnDied += OnTargetDied;

        // Highlight new nameplate
        _currentNameplate = health.GetComponentInChildren<EnemyHealthBarUI>();
        if (_currentNameplate != null)
            _currentNameplate.SetTargeted(true);

        // Apply bar colour from disposition
        ApplyColour();

        // Set name
        if (targetNameText != null)
            targetNameText.text = health.gameObject.name;

        // Show the frame
        ShowPanel();

        // Force an immediate refresh
        Refresh(health.CurrentHealth, health.MaxHealth);
    }

private void ClearTarget()
    {
        if (_currentTarget != null)
        {
            _currentTarget.OnHealthChanged -= Refresh;
            _currentTarget.OnDied -= OnTargetDied;
        }

        if (_currentNameplate != null)
            _currentNameplate.SetTargeted(false);

        _currentTarget = null;
        _currentController = null;
        _currentNameplate = null;

        HidePanel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Event Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void Refresh(float current, float max)
    {
        float percent = max > 0f ? current / max : 0f;

        if (barFill != null)
            barFill.fillAmount = percent;

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void OnTargetDied(GameObject source)
    {
        // Keep the target frame visible briefly so the player sees "0 / 100",
        // then auto-clear after a short delay.
        Invoke(nameof(ClearTarget), 2.5f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyColour()
    {
        if (barFill == null || _currentController == null) return;

        barFill.color = _currentController.Disposition switch
        {
            EnemyDisposition.Hostile => hostileColor,
            EnemyDisposition.Neutral => neutralColor,
            EnemyDisposition.Passive => friendlyColor,
            _ => hostileColor,
        };
    }

    /// <summary>
    /// Expose current target for other systems (e.g. attack system).
    /// Returns null if no target is selected.
    /// </summary>
    public HealthComponent CurrentTarget => _currentTarget;


// ─────────────────────────────────────────────────────────────────────────
    //  Panel Show / Hide (without disabling this GameObject)
    // ─────────────────────────────────────────────────────────────────────────

    private void ShowPanel()
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(true);
    }

    private void HidePanel()
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
    }
}