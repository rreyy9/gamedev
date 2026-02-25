using UnityEngine;
using System.Collections;

/// <summary>
/// A mineable rock node. Implements IInteractable.
/// Handles the mining cast bar and animation only.
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
    [SerializeField] private GameObject depletedVisuals; // optional greyed-out rock mesh

    // Runtime state
    private bool _isDepleted = false;
    private bool _isMining = false;
    private LootSource _lootSource;

    // ─────────────────────────────────────────────
    //  IInteractable
    // ─────────────────────────────────────────────

    // Simple — player always mines, no loot window shortcut
    public string InteractionPrompt => _isDepleted ? $"{nodeName} (Depleted)" : $"Mine {nodeName}";

    // Simple — only blocked while actively mining or fully depleted waiting for respawn
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

        StartCoroutine(MiningRoutine());
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
        {
            Debug.LogWarning($"[MiningNode] '{gameObject.name}' has no LootSource component!");
            return;
        }

        // Subscribe to LootSource events so we stay in sync
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
        // LootSource handles hiding the mesh and starting respawn timer.
        // _isDepleted is already true from MiningRoutine, nothing extra needed.
        Debug.Log($"[MiningNode] '{gameObject.name}' depleted. Waiting for LootSource respawn.");
    }

    private void HandleRespawned()
    {
        _isDepleted = false;
        if (depletedVisuals != null) depletedVisuals.SetActive(false);
        Debug.Log($"[MiningNode] '{gameObject.name}' ready to mine again.");
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

        yield return new WaitForSeconds(miningDuration);

        gatherController?.StopMining();
        _isMining = false;
        _isDepleted = true;

        if (depletedVisuals != null) depletedVisuals.SetActive(true);

        // Hand off to LootSource — opens loot window with existing loot
        // (loot was already generated on first mine and persists until taken)
        if (_lootSource != null)
            _lootSource.Interact();
    }

    private void HandleLootWindowClosed()
    {
        // If the window closed but loot remains (player closed without looting),
        // reset depleted so they can mine again — fresh loot rolls on next mine
        if (_isDepleted && _lootSource != null && _lootSource.HasLoot)
        {
            _isDepleted = false;
            if (depletedVisuals != null) depletedVisuals.SetActive(false);
            Debug.Log($"[MiningNode] Loot window closed without looting — ready to mine again.");
        }
    }
}