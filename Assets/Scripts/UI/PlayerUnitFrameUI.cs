using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerUnitFrameUI — WoW-Style Player Unit Frame (HUD)
/// Unity 6000.3.8f1 | No deprecated packages
///
/// Renders the player's health as a flat filled bar (Image fill, not a Slider),
/// with name text and HP numbers — matching WoW's unit frame style.
///
/// This REPLACES PlayerHealthUI. Delete PlayerHealthUI from your HUD.
///
/// PREFAB / HIERARCHY SETUP:
///
///   PlayerUnitFrame (RectTransform, anchor: bottom-left)
///   ├── PortraitFrame (Image — dark border box, ~80×80)
///   │   └── PortraitIcon (Image — placeholder portrait / class icon)
///   ├── FrameBody (RectTransform — sits right of portrait)
///   │   ├── PlayerNameText (TMP_Text — player or character name)
///   │   ├── HealthBarBackground (Image — dark grey track, same width as fill)
///   │   │   └── HealthBarFill (Image — Filled, Horizontal) ← assign this
///   │   └── HPText (TMP_Text — "85 / 100" centred on bar)
///   └── (optional) LevelText (TMP_Text)
///
///   Attach this script to the PlayerUnitFrame root.
///   Assign BarFill and PlayerNameText in the Inspector.
///   HealthComponent is auto-found from the GameObject tagged "Player".
///
/// INSPECTOR QUICK SETUP CHECKLIST:
///   ☑ BarFill Image → Image Type: Filled, Fill Method: Horizontal, Fill Origin: Left
///   ☑ BarFill Image → Preserve Aspect: OFF
///   ☑ HPText → alignment: Centre Middle
///   ☑ Player GameObject tagged "Player"
/// </summary>
public class PlayerUnitFrameUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Auto-found if null — searches for GameObject tagged 'Player'.")]
    [SerializeField] private HealthComponent playerHealth;

    [Header("Bar")]
    [Tooltip("Image with Type=Filled, Method=Horizontal. This IS the health bar fill.")]
    [SerializeField] private Image barFill;

    [Header("Text")]
    [Tooltip("Shows the character / player name.")]
    [SerializeField] private TMP_Text playerNameText;

    [Tooltip("(Optional) Shows 'current / max' HP numbers, ideally overlaid on the bar.")]
    [SerializeField] private TMP_Text hpText;

    [Tooltip("(Optional) Level number text.")]
    [SerializeField] private TMP_Text levelText;

    [Header("Bar Colours")]
    [Tooltip("Colour at full health. WoW uses a saturated green.")]
    [SerializeField] private Color fullHealthColor = new Color(0.20f, 0.73f, 0.20f);

    [Tooltip("Colour at mid health.")]
    [SerializeField] private Color midHealthColor = new Color(0.90f, 0.65f, 0.10f);

    [Tooltip("Colour at low health. WoW flashes red.")]
    [SerializeField] private Color lowHealthColor = new Color(0.82f, 0.10f, 0.10f);

    [Tooltip("Below this fraction the bar turns low-health colour.")]
    [SerializeField] private float lowThreshold = 0.30f;

    [Tooltip("Below this fraction the bar turns mid-health colour.")]
    [SerializeField] private float midThreshold = 0.60f;

    [Header("Player Info")]
    [Tooltip("Name shown in the unit frame. If blank, uses GameObject name.")]
    [SerializeField] private string characterName = "";

    [SerializeField] private int characterLevel = 1;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (playerHealth == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                playerHealth = player.GetComponent<HealthComponent>();
        }

        if (playerHealth == null)
        {
            Debug.LogError("[PlayerUnitFrameUI] No HealthComponent found on Player.", this);
            return;
        }

        // ── Static labels ─────────────────────────────────────────────────────
        if (playerNameText != null)
            playerNameText.text = string.IsNullOrEmpty(characterName)
                ? playerHealth.gameObject.name
                : characterName;

        if (levelText != null)
            levelText.text = characterLevel.ToString();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += Refresh;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;
    }

    private void Start()
    {
        if (playerHealth != null)
            Refresh(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Event Handler
    // ─────────────────────────────────────────────────────────────────────────

    private void Refresh(float current, float max)
    {
        float percent = max > 0f ? current / max : 0f;

        // ── Fill amount ───────────────────────────────────────────────────────
        if (barFill != null)
        {
            barFill.fillAmount = percent;
            barFill.color = percent <= lowThreshold ? lowHealthColor
                          : percent <= midThreshold ? midHealthColor
                          : fullHealthColor;
        }

        // ── HP text ───────────────────────────────────────────────────────────
        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}