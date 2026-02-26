using UnityEngine;
using System.Collections;

/// <summary>
/// A mineable rock node. Implements IInteractable.
/// Handles the mining cast bar, animation, and cancellation.
/// LootSource handles loot generation, mesh visibility, and respawn timing.
/// </summary>
public class MiningNode : MonoBehaviour, IInteractable
{
    [Header("Node Settings")]
    [SerializeField] private string nodeName = "Copper Ore";
    [SerializeField] private float miningDuration = 3f;

    [Header("Required Tool")]
    [SerializeField] private ItemCategory requiredToolCategory = ItemCategory.Tool;

    [Header("Visuals")]
    [SerializeField] private GameObject highlightObject;
    [SerializeField] private GameObject depletedVisuals;

    // Runtime state
    private bool _isDepleted = false;
    private bool _isMining = false;
    private LootSource _lootSource;
    private Coroutine _miningCoroutine;

    // ─────────────────────────────────────────────
    //  IInteractable
    // ─────────────────────────────────────────────

    public string InteractionPrompt => _isDepleted ? $"{nodeName} (Depleted)" : $"Mine {nodeName}";
    public bool CanInteract => !_isMining && !_isDepleted;
    public Transform InteractableTransform => transform;

    public void Interact()
    {
        if (!CanInteract) return;

        if (EquipmentManager.CurrentlyEquippedItem == null ||
            EquipmentManager.CurrentlyEquippedItem.category != requiredToolCategory)
        {
            Debug.Log($"[MiningNode] You need a {requiredToolCategory} equipped to mine {nodeName}.");
            return;
        }

        _miningCoroutine = StartCoroutine(MiningRoutine());
    }

    public void SetHighlight(bool active)
    {
        if (highlightObject != null)
            highlightObject.SetActive(active);
    }

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        LootUIManager.OnLootWindowClosed += HandleLootWindowClosed;
        _lootSource = GetComponent<LootSource>();

        if (_lootSource == null)
            Debug.LogWarning($"[MiningNode] '{gameObject.name}' has no LootSource component! Loot window won't open.");

        _lootSource.OnLooted += HandleFullyLooted;
        _lootSource.OnRespawned += HandleRespawned;
    }

    private void OnDestroy()
    {
        LootUIManager.OnLootWindowClosed -= HandleLootWindowClosed;
        if (_lootSource != null)
        {
            _lootSource.OnLooted -= HandleFullyLooted;
            _lootSource.OnRespawned -= HandleRespawned;
        }
    }

    // ─────────────────────────────────────────────
    //  LootSource Event Handlers
    // ─────────────────────────────────────────────

    private void HandleFullyLooted()
    {
        Debug.Log($"[MiningNode] '{gameObject.name}' depleted. Waiting for LootSource respawn.");
    }

    private void HandleRespawned()
    {
        _isDepleted = false;
        if (depletedVisuals != null) depletedVisuals.SetActive(false);
        Debug.Log($"[MiningNode] '{gameObject.name}' ready to mine again.");
    }

    private void HandleLootWindowClosed()
    {
        if (_isDepleted && _lootSource != null && _lootSource.HasLoot)
        {
            _isDepleted = false;
            if (depletedVisuals != null) depletedVisuals.SetActive(false);
            Debug.Log($"[MiningNode] Loot window closed without looting — ready to mine again.");
        }
    }

    // ─────────────────────────────────────────────
    //  Mining Logic
    // ─────────────────────────────────────────────

    private IEnumerator MiningRoutine()
    {
        _isMining = true;

        var player = GameObject.FindWithTag("Player");
        var gatherController = player?.GetComponent<PlayerGatheringController>();
        gatherController?.StartMining(miningDuration);

        float elapsed = 0f;

        // ── Active wait loop — checks for cancel each frame ──
        while (elapsed < miningDuration)
        {
            elapsed += Time.deltaTime;

            // Check Escape key
            if (gatherController != null && gatherController.IsCancelRequested)
            {
                CancelMining(gatherController, "Escape pressed");
                yield break; // Exit coroutine immediately
            }

            // Check player movement
            if (gatherController != null && gatherController.HasPlayerMoved)
            {
                CancelMining(gatherController, "Player moved");
                yield break;
            }

            yield return null; // Wait one frame, then check again
        }

        // ── Full duration completed — mining succeeded ──
        gatherController?.StopMining();
        _isMining = false;
        _isDepleted = true;

        if (depletedVisuals != null) depletedVisuals.SetActive(true);

        if (_lootSource != null)
            _lootSource.Interact();
    }

    private void CancelMining(PlayerGatheringController gatherController, string reason)
    {
        Debug.Log($"[MiningNode] Mining cancelled — {reason}.");
        gatherController?.StopMining();
        _isMining = false;
        // _isDepleted stays false — node is still mineable
        // depletedVisuals stay hidden — rock looks untouched
        _miningCoroutine = null;
    }
}