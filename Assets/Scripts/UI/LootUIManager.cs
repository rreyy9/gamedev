using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the loot window UI panel. Singleton.
/// Opens/closes the loot window, populates slots from a LootSource,
/// handles Loot All, auto-close on distance, and Escape/E to close.
/// </summary>
public class LootUIManager : MonoBehaviour
{
    public static LootUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject lootWindowPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject lootSlotPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button lootAllButton;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private float maxLootDistance = 4f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Runtime
    private LootSource currentSource;
    private List<LootSlotUI> activeSlots = new List<LootSlotUI>();
    private List<GameObject> slotPool = new List<GameObject>();
    private PlayerInputActions inputActions;

    public bool IsOpen => lootWindowPanel != null && lootWindowPanel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LootUIManager] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (lootAllButton != null)
            lootAllButton.onClick.AddListener(OnLootAllClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseLootWindow);

        if (lootWindowPanel != null)
            lootWindowPanel.SetActive(false);

        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.UI.CloseLootWindow.performed += OnCloseLootInput;
            inputActions.UI.Enable();
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.UI.CloseLootWindow.performed -= OnCloseLootInput;
            inputActions.UI.Disable();
        }
    }

    private void Update()
    {
        if (IsOpen && currentSource != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(player.transform.position, currentSource.transform.position);
                if (distance > maxLootDistance)
                {
                    if (enableDebugLogs)
                        Debug.Log("[LootUIManager] Player moved out of range, closing.");
                    CloseLootWindow();
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    public void OpenLootWindow(LootSource source)
    {
        if (source == null) return;

        if (enableDebugLogs)
            Debug.Log($"[LootUIManager] Opening loot window for '{source.DisplayName}'");

        if (IsOpen)
            ClearSlots();

        currentSource = source;

        if (titleText != null)
            titleText.text = source.DisplayName;

        PopulateSlots();

        if (lootWindowPanel != null)
            lootWindowPanel.SetActive(true);
    }

    public void CloseLootWindow()
    {
        if (enableDebugLogs)
            Debug.Log("[LootUIManager] Closing loot window.");

        // Always hide the tooltip — OnPointerExit won't fire if the window
        // closes while the cursor is still over a slot.
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();

        if (lootWindowPanel != null)
            lootWindowPanel.SetActive(false);

        ClearSlots();

        if (currentSource != null)
        {
            currentSource.SetHighlight(false);
            currentSource = null;
        }
    }

    public void RefreshLootWindow()
    {
        if (currentSource == null || !IsOpen) return;

        if (!currentSource.HasLoot)
        {
            if (enableDebugLogs)
                Debug.Log("[LootUIManager] All loot taken, closing window.");
            CloseLootWindow();
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[LootUIManager] Refreshing ({currentSource.GetLootContents().Count} items remaining)");

        ClearSlots();
        PopulateSlots();
    }

    // ─────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────

    private void PopulateSlots()
    {
        if (currentSource == null || lootSlotPrefab == null || slotContainer == null)
            return;

        var contents = currentSource.GetLootContents();

        for (int i = 0; i < contents.Count; i++)
        {
            if (contents[i] == null || contents[i].itemData == null || contents[i].quantity <= 0)
                continue;

            GameObject slotObj = GetOrCreateSlot(activeSlots.Count);
            var slotUI = slotObj.GetComponent<LootSlotUI>();

            if (slotUI != null)
            {
                slotUI.Setup(i, contents[i], currentSource);
                activeSlots.Add(slotUI);
            }
        }
    }

    private GameObject GetOrCreateSlot(int poolIndex)
    {
        if (poolIndex < slotPool.Count)
        {
            slotPool[poolIndex].SetActive(true);
            return slotPool[poolIndex];
        }

        var obj = Instantiate(lootSlotPrefab, slotContainer);
        slotPool.Add(obj);
        return obj;
    }

    private void ClearSlots()
    {
        activeSlots.Clear();
        for (int i = 0; i < slotPool.Count; i++)
        {
            if (slotPool[i] != null)
                slotPool[i].SetActive(false);
        }
    }

    private void OnLootAllClicked()
    {
        if (currentSource == null) return;

        if (enableDebugLogs)
            Debug.Log("[LootUIManager] Loot All clicked.");

        int overflow = currentSource.TakeAllLoot();

        if (overflow > 0)
            Debug.Log($"Inventory full! {overflow} items couldn't be picked up.");

        RefreshLootWindow();
    }

    private void OnCloseLootInput(InputAction.CallbackContext context)
    {
        if (IsOpen)
            CloseLootWindow();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (lootAllButton != null)
            lootAllButton.onClick.RemoveListener(OnLootAllClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseLootWindow);

        inputActions?.Dispose();
    }
}