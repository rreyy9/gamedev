using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EnemyHealthBarUI — WoW-Style Floating Name Plate
/// Unity 6000.3.8f1 | No deprecated packages
///
/// Floats above an enemy's head. Shows the enemy's name and a flat filled
/// health bar (Image fill — NOT a Slider). Hostile enemies show red bars,
/// neutral enemies show yellow — matching WoW's nameplate conventions.
///
/// Visibility rules (in priority order):
///   1. Enemy aggroes (Idle → Chase/Attack)  → show immediately, stay visible
///   2. Enemy takes damage                   → show immediately, reset fade timer
///   3. Enemy leashes (→ ReturnToPost)       → wait leashHideDelay seconds,
///                                              then hide and restore full HP
///   4. alwaysVisible = true                 → always shown regardless
///
/// Billboards to face the camera in LateUpdate.
///
/// PREFAB SETUP:
///   Root Canvas (World Space, Scale ~0.01)
///   └── NameText (TMP_Text)
///   └── BarBackground (Image, dark)
///       └── BarFill (Image, Filled, Horizontal)
///
///   1. Set Canvas Render Mode: World Space
///   2. Canvas Width: 200, Height: 40  →  Scale: 0.01, 0.01, 0.01
///   3. Position ~2.2 units above enemy root (Y offset in prefab)
///   4. Attach this script to the root Canvas
///   5. Assign NameText, BarFill, CanvasGroup in Inspector
/// </summary>
public class EnemyHealthBarUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI References")]
    [Tooltip("TMP_Text showing the enemy's name above the bar.")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("The health fill Image. Set Image Type = Filled, Fill Method = Horizontal.")]
    [SerializeField] private Image barFill;

    [Tooltip("Optional TMP_Text showing 'current / max' HP numbers inside or below the bar.")]
    [SerializeField] private TMP_Text healthText;

    [Tooltip("CanvasGroup on this Canvas — used for fade in/out.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Bar Colours — WoW Convention")]
    [Tooltip("Hostile enemies (Disposition.Hostile). WoW uses red.")]
    [SerializeField] private Color hostileColor = new Color(0.78f, 0.10f, 0.10f);

    [Tooltip("Neutral enemies (Disposition.Neutral). WoW uses yellow.")]
    [SerializeField] private Color neutralColor = new Color(0.90f, 0.80f, 0.10f);

    [Tooltip("Passive / friendly. WoW uses green.")]
    [SerializeField] private Color friendlyColor = new Color(0.10f, 0.78f, 0.20f);

    [Header("Visibility")]
    [Tooltip("Show the bar even at full health (WoW always-on nameplate mode).")]
    [SerializeField] private bool alwaysVisible = false;

    [Tooltip("Seconds the bar stays visible after damage before fading (non-aggro).")]
    [SerializeField] private float visibilityDuration = 5f;

    [Tooltip("Alpha fade speed when hiding the bar.")]
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Aggro / Leash")]
    [Tooltip("Seconds after leashing before the nameplate hides and HP resets.")]
    [SerializeField] private float leashHideDelay = 3f;

    [Tooltip("Seconds for HP to smoothly restore to full after leashing.")]
    [SerializeField] private float healthRestoreSpeed = 10f;

    [Header("Billboard")]
    [SerializeField] private bool billboardToCamera = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private HealthComponent _health;
    private EnemyController _controller;
    private Camera _mainCamera;

    // Damage-based visibility (used when not aggroed)
    private float _visibilityTimer;
    private bool _isFading;

    // Aggro state
    private bool _isAggroed;

    // Leash state
    private bool _isLeashing;
    private float _leashTimer;
    private bool _isRestoringHealth;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _health = GetComponentInParent<HealthComponent>();
        _controller = GetComponentInParent<EnemyController>();
        _mainCamera = Camera.main;

        if (_health == null)
            Debug.LogError("[EnemyHealthBarUI] No HealthComponent found in parent hierarchy.", this);

        ApplyDispositionColour();

        if (nameText != null)
            nameText.text = _health != null ? _health.gameObject.name : "Enemy";

        // Start hidden unless alwaysVisible
        if (canvasGroup != null)
            canvasGroup.alpha = alwaysVisible ? 1f : 0f;
    }

    private void Start()
    {
        // Initial bar fill sync
        if (_health != null)
            Refresh(_health.CurrentHealth, _health.MaxHealth);

        // Subscribe to state machine events
        if (_controller != null)
        {
            _controller.OnAggroed += HandleAggroed;
            _controller.OnLeashed += HandleLeashed;
        }
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

    private void OnDestroy()
    {
        if (_controller != null)
        {
            _controller.OnAggroed -= HandleAggroed;
            _controller.OnLeashed -= HandleLeashed;
        }
    }

    private void LateUpdate()
    {
        // ── Billboard ─────────────────────────────────────────────────────────
        if (billboardToCamera && _mainCamera != null)
            transform.rotation = _mainCamera.transform.rotation;

        // ── Leash countdown → hide + restore HP ───────────────────────────────
        if (_isLeashing)
        {
            _leashTimer -= Time.deltaTime;
            if (_leashTimer <= 0f)
            {
                _isLeashing = false;
                _isRestoringHealth = true;
                StartFade();
            }
        }

        // ── Smooth HP restore after leash hide ────────────────────────────────
        if (_isRestoringHealth && _health != null)
        {
            float newHP = Mathf.MoveTowards(
                _health.CurrentHealth,
                _health.MaxHealth,
                healthRestoreSpeed * Time.deltaTime
            );

            // Directly set health without broadcasting OnHealthChanged
            // so the bar doesn't re-show while fading/hidden
            _health.SetHealthSilent(newHP);

            // Keep the fill image in sync visually (silent — no show trigger)
            if (barFill != null)
                barFill.fillAmount = _health.MaxHealth > 0f
                    ? _health.CurrentHealth / _health.MaxHealth
                    : 0f;

            if (Mathf.Approximately(_health.CurrentHealth, _health.MaxHealth))
                _isRestoringHealth = false;
        }

        // ── Damage-based fade (only when not aggroed) ─────────────────────────
        if (!_isAggroed && !alwaysVisible)
        {
            if (_isFading && canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.MoveTowards(
                    canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
                if (canvasGroup.alpha <= 0f)
                    _isFading = false;
            }
            else if (_visibilityTimer > 0f)
            {
                _visibilityTimer -= Time.deltaTime;
                if (_visibilityTimer <= 0f)
                    _isFading = true;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyController State Events
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleAggroed()
    {
        _isAggroed = true;
        _isLeashing = false;    // Cancel any pending leash hide
        _isRestoringHealth = false;
        _isFading = false;
        _visibilityTimer = 0f;

        ShowNameplate();
    }

    private void HandleLeashed()
    {
        _isAggroed = false;
        _isLeashing = true;
        _leashTimer = leashHideDelay;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HealthComponent Events
    // ─────────────────────────────────────────────────────────────────────────

    private void Refresh(float current, float max)
    {
        float percent = max > 0f ? current / max : 0f;

        if (barFill != null)
            barFill.fillAmount = percent;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        // Only show + reset timer if not already being kept up by aggro
        if (!_isAggroed && canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            _isFading = false;
            _visibilityTimer = visibilityDuration;
        }
    }

    private void OnDied(GameObject source)
    {
        _isAggroed = false;
        _isLeashing = false;
        _isRestoringHealth = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            _isFading = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void ShowNameplate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            _isFading = false;
        }
    }

    private void StartFade()
    {
        if (!alwaysVisible)
            _isFading = true;
    }

    private void ApplyDispositionColour()
    {
        if (barFill == null || _controller == null) return;

        barFill.color = _controller.Disposition switch
        {
            EnemyDisposition.Hostile => hostileColor,
            EnemyDisposition.Neutral => neutralColor,
            EnemyDisposition.Passive => friendlyColor,
            _ => hostileColor,
        };
    }

    /// <summary>
    /// Called by the targeting system to bold/unbold the name text.
    /// </summary>
    public void SetTargeted(bool targeted)
    {
        if (nameText != null)
            nameText.fontStyle = targeted ? FontStyles.Bold : FontStyles.Normal;
    }
}