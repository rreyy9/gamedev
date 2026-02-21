using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerHealthUI — Player HUD Health Bar
/// Unity 6000.3.8f1 | No deprecated packages
///
/// Subscribes to the player's HealthComponent events and updates the HUD.
/// Uses a Unity UI Slider as the health bar — no polling in Update, fully event-driven.
///
/// SETUP:
///   1. Create a Canvas (Screen Space - Overlay) if you don't have one.
///   2. Add a Slider GameObject inside the Canvas for the health bar.
///      • Set Min Value: 0, Max Value: 1, Whole Numbers: OFF
///      • Remove the "Handle Slide Area" child (we don't need a draggable handle)
///      • Style the Fill with your health bar colour (e.g. red)
///   3. (Optional) Add a TMP_Text element to show "85 / 100" style text.
///   4. Attach this script to the Canvas or a UI manager GameObject.
///   5. Assign the Slider and PlayerHealth references in the Inspector.
///
/// ALTERNATIVE: If you prefer UI Toolkit (UIDocument), replace the Slider
///   reference with a ProgressElement and update accordingly.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The player's HealthComponent. Auto-found if left null (searches tag 'Player').")]
    [SerializeField] private HealthComponent _playerHealth;

    [Header("UI Elements")]
    [Tooltip("Slider used as the health bar fill. Set Min=0, Max=1, Whole Numbers=OFF.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("(Optional) Text element showing 'current / max' HP numbers.")]
    [SerializeField] private TMP_Text healthText;

    [Tooltip("(Optional) Image that changes colour based on health percentage.")]
    [SerializeField] private Image fillImage;

    [Header("Colour Settings")]
    [SerializeField] private Color fullHealthColour = new Color(0.2f, 0.8f, 0.2f); // Green
    [SerializeField] private Color midHealthColour = new Color(1.0f, 0.7f, 0.0f); // Orange
    [SerializeField] private Color lowHealthColour = new Color(0.9f, 0.1f, 0.1f); // Red
    [Tooltip("Health % below which the bar shows 'low health' colour.")]
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [Tooltip("Health % below which the bar shows 'mid health' colour.")]
    [SerializeField] private float midHealthThreshold = 0.6f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-find player health if not assigned
        if (_playerHealth == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                _playerHealth = player.GetComponent<HealthComponent>();
        }

        if (_playerHealth == null)
            Debug.LogError("[PlayerHealthUI] No HealthComponent found on Player. " +
                           "Assign it in the Inspector or ensure Player is tagged 'Player'.", this);
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged += Refresh;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged -= Refresh;
    }

    private void Start()
    {
        // Force an initial update so the bar shows full HP at game start
        if (_playerHealth != null)
            Refresh(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Update UI
    // ─────────────────────────────────────────────────────────────────────────

    private void Refresh(float current, float max)
    {
        float percent = max > 0f ? current / max : 0f;

        // ── Slider ────────────────────────────────────────────────────────────
        if (healthSlider != null)
            healthSlider.value = percent;

        // ── Text ──────────────────────────────────────────────────────────────
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        // ── Colour ────────────────────────────────────────────────────────────
        if (fillImage != null)
            fillImage.color = GetHealthColour(percent);
    }

    private Color GetHealthColour(float percent)
    {
        if (percent <= lowHealthThreshold)
            return lowHealthColour;
        if (percent <= midHealthThreshold)
            return Color.Lerp(lowHealthColour, midHealthColour,
                              (percent - lowHealthThreshold) / (midHealthThreshold - lowHealthThreshold));
        return Color.Lerp(midHealthColour, fullHealthColour,
                          (percent - midHealthThreshold) / (1f - midHealthThreshold));
    }
}