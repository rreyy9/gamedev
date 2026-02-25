using UnityEngine;
using System.Collections;

/// <summary>
/// A mineable rock node. Implements IInteractable to hook into the existing
/// PlayerInteraction system. When interacted with, plays the mining animation
/// for a set duration, then opens the loot window via LootSource.
/// </summary>
public class MiningNode : MonoBehaviour, IInteractable
{
    [Header("Node Settings")]
    [SerializeField] private string nodeName = "Copper Ore";
    [SerializeField] private float miningDuration = 3f;
    [SerializeField] private float respawnTime = 30f;

    [Header("Visuals")]
    [SerializeField] private GameObject nodeVisuals;        // The rock mesh to hide when depleted
    [SerializeField] private GameObject highlightObject;    // Optional glow/outline object

    // Runtime state
    private bool _isDepleted = false;
    private bool _isMining = false;
    private LootSource _lootSource;

    // ─────────────────────────────────────────────
    //  IInteractable Implementation
    // ─────────────────────────────────────────────

    public string InteractionPrompt => $"Mine {nodeName}";

    // CanInteract matches the interface name used in your IInteractable
    public bool CanInteract => !_isDepleted && !_isMining;

    // Required by IInteractable for distance checks in PlayerInteraction
    public Transform InteractableTransform => transform;

    // No parameters — matches your IInteractable signature exactly
    public void Interact()
    {
        if (!CanInteract) return;
        StartCoroutine(MiningRoutine());
    }

    // Called by PlayerInteraction to show/hide the highlight on this object
    public void SetHighlight(bool active)
    {
        if (highlightObject != null)
            highlightObject.SetActive(active);
    }

    // ─────────────────────────────────────────────
    //  Mining Logic
    // ─────────────────────────────────────────────

    private IEnumerator MiningRoutine()
    {
        _isMining = true;

        // Find the player and tell them to start the mining animation
        // PlayerGatheringController sits on the Player GameObject
        var player = GameObject.FindWithTag("Player");
        var gatherController = player?.GetComponent<PlayerGatheringController>();
        gatherController?.StartMining(miningDuration);

        // Wait the full mining duration
        yield return new WaitForSeconds(miningDuration);

        // Stop the animation
        gatherController?.StopMining();

        _isMining = false;
        _isDepleted = true;

        // Open the loot window using the existing LootSource on this same GameObject
        // LootSource.Interact() takes no arguments — it handles loot generation itself
        if (_lootSource != null)
        {
            _lootSource.Interact();
        }

        // Hide the rock visuals
        if (nodeVisuals != null)
            nodeVisuals.SetActive(false);

        // Start the respawn countdown
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        _isDepleted = false;

        if (nodeVisuals != null)
            nodeVisuals.SetActive(true);
    }

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        // Cache the LootSource on this same GameObject
        _lootSource = GetComponent<LootSource>();

        if (_lootSource == null)
            Debug.LogWarning($"[MiningNode] '{gameObject.name}' has no LootSource component! Loot window won't open.");
    }
}