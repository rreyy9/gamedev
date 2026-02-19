using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemyHealthBarUI — World-Space Enemy Health Bar
/// Unity 6000.3.8f1 | No deprecated packages
///
/// Floats above an enemy's head and shows their current health.
/// Hides when at full health, shows when damaged, fades out after a delay.
/// Subscribes to the enemy's HealthComponent events — zero Update polling.
///
/// SETUP:
///   1. Create a Canvas component:
///      • Render Mode: World Space
///      • Event Camera: your main camera
///      • Width: 1, Height: 0.15 (small, floats above head)
///      • Scale: 0.01 on all axes
///   2. Inside the Canvas, add a Slider:
///      • Min: 0, Max: 1, Whole Numbers: OFF
///      • Remove Handle Slide Area
///      • Style the fill (red bar works well for enemies)
///   3. Place this script on the Canvas GameObject.
///   4. The HealthComponent is auto-found from the parent enemy.
///
/// PREFAB TIP:
///   Save your enemy health bar Canvas as a prefab and drag it into
///   each enemy prefab as a child. Position it ~2 units above the root (Y offset).
///   The script handles the rest automatically.
/// </summary>
public class EnemyHealthBarUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Visibility Settings")]
    [Tooltip("Always show the health bar, even at full health.")]
    [SerializeField] private bool alwaysVisible = false;

    [Tooltip("How long (seconds) the bar stays visible after taking damage.")]
    [SerializeField] private float visibilityDuration = 4f;

    [Tooltip("How quickly the bar fades out.")]
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Billboard")]
    [Tooltip("Rotate the bar to always face the camera (recommended for world-space canvas).")]
    [SerializeField] private bool billboardToCamera = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    private HealthComponent _health;
    private Camera _mainCamera;
    private float _visibilityTimer;
    private bool _isFading;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-find HealthComponent on the parent enemy
        _health = GetComponentInParent<HealthComponent>();
        _mainCamera = Camera.main;

        if (_health == null)
            Debug.LogError("[EnemyHealthBarUI] No HealthComponent found in parent hierarchy.", this);

        // Start hidden (will show on first damage)
        if (canvasGroup != null && !alwaysVisible)
            canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        if (_health == null) return;
        _health.OnHealthChanged += Refresh;
        _health.OnDied += OnDied;
    }

    private void OnDisable()
    {
        if (_health == null) return;
        _health.OnHealthChanged -= Refresh;
        _health.OnDied -= OnDied;
    }

    private void Start()
    {
        if (_health != null)
            Refresh(_health.CurrentHealth, _health.MaxHealth);
    }

    private void LateUpdate()
    {
        // ── Billboard rotation ────────────────────────────────────────────────
        if (billboardToCamera && _mainCamera != null)
            transform.rotation = _mainCamera.transform.rotation;

        // ── Fade out timer ────────────────────────────────────────────────────
        if (_isFading && canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
            if (canvasGroup.alpha <= 0f)
                _isFading = false;
        }
        else if (!alwaysVisible && _visibilityTimer > 0f)
        {
            _visibilityTimer -= Time.deltaTime;
            if (_visibilityTimer <= 0f)
                _isFading = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Event Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void Refresh(float current, float max)
    {
        float percent = max > 0f ? current / max : 0f;

        if (healthSlider != null)
            healthSlider.value = percent;

        // Show the bar and reset the fade timer
        if (!alwaysVisible && canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            _isFading = false;
            _visibilityTimer = visibilityDuration;
        }
    }

    private void OnDied(GameObject source)
    {
        // Hide immediately on death — the corpse doesn't need a health bar
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            _isFading = false;
        }
    }
}