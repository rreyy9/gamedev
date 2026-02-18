using UnityEngine;
using TMPro;

/// <summary>
/// Displays interaction prompts like "Press [E] Loot Goblin" at the bottom of the screen.
/// Driven by PlayerInteraction events.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private string keyLabel = "E";

    // Runtime
    private bool isShowing = false;
    private PlayerInteraction playerInteraction;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInteraction = player.GetComponent<PlayerInteraction>();
            if (playerInteraction != null)
            {
                playerInteraction.OnInteractableFound += ShowPrompt;
                playerInteraction.OnInteractableLost += HidePrompt;
            }
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if (canvasGroup == null) return;

        float targetAlpha = isShowing ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }

    private void ShowPrompt(string actionText)
    {
        isShowing = true;

        if (promptText != null)
            promptText.text = $"Press [{keyLabel}] {actionText}";
    }

    private void HidePrompt()
    {
        isShowing = false;
    }

    private void OnDestroy()
    {
        if (playerInteraction != null)
        {
            playerInteraction.OnInteractableFound -= ShowPrompt;
            playerInteraction.OnInteractableLost -= HidePrompt;
        }
    }
}