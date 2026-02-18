using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central manager that owns the player's inventory and action bar data.
/// Attach this to the Player GameObject (or a persistent manager object).
/// Handles the toggle input for opening/closing the inventory panel.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int inventorySlotCount = 20;
    [SerializeField] private int actionBarSlotCount = 9;

    [Header("UI References")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private ActionBarUI actionBarUI;

    [Header("Input")]
    [SerializeField] private PlayerInputActions inputActions;

    // The data containers
    public Inventory PlayerInventory { get; private set; }
    public Inventory ActionBar { get; private set; }

    // Singleton for easy access (optional, remove if you prefer dependency injection)
    public static InventoryManager Instance { get; private set; }

    private bool isInventoryOpen = false;

    private void Awake()
    {
        // Simple singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create the inventory data
        PlayerInventory = new Inventory(inventorySlotCount);
        ActionBar = new Inventory(actionBarSlotCount);

        // Create input actions
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();
        inputActions.UI.ToggleInventory.performed += OnToggleInventory;

        // Subscribe to action bar number key usage
        inputActions.UI.ActionBarUse.performed += OnActionBarUse;
    }

    private void OnDisable()
    {
        inputActions.UI.ToggleInventory.performed -= OnToggleInventory;
        inputActions.UI.ActionBarUse.performed -= OnActionBarUse;
        inputActions.UI.Disable();
    }

    private void Start()
    {
        // Initialize UI with data
        if (inventoryUI != null)
        {
            inventoryUI.Initialize(PlayerInventory);
            inventoryUI.gameObject.SetActive(false); // Start closed
        }

        if (actionBarUI != null)
        {
            actionBarUI.Initialize(ActionBar);
            // Action bar is always visible
        }
    }

    // ─────────────────────────────────────────────
    //  Input Handlers
    // ─────────────────────────────────────────────

    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryUI != null)
        {
            inventoryUI.gameObject.SetActive(isInventoryOpen);
        }

        // Optional: Show/hide cursor when inventory is open
        Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnActionBarUse(InputAction.CallbackContext context)
    {
        // The action bar use sends a float value 1-9 based on which key was pressed
        float value = context.ReadValue<float>();
        int slotIndex = Mathf.RoundToInt(value) - 1; // Convert 1-9 to 0-8

        if (slotIndex >= 0 && slotIndex < actionBarSlotCount)
        {
            UseActionBarSlot(slotIndex);
        }
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Adds an item to the player's inventory. Tries inventory first.
    /// Returns the overflow count (0 = all added).
    /// </summary>
    public int AddItemToInventory(ItemData itemData, int quantity = 1)
    {
        int overflow = PlayerInventory.AddItem(itemData, quantity);

        if (overflow > 0)
        {
            Debug.LogWarning($"Inventory full! Could not add {overflow}x {itemData.itemName}");
        }

        return overflow;
    }

    /// <summary>
    /// Attempts to use/consume the item in the given action bar slot.
    /// </summary>
    public void UseActionBarSlot(int slotIndex)
    {
        var stack = ActionBar.GetSlot(slotIndex);
        if (stack == null || !stack.IsValid) return;

        if (stack.itemData.isConsumable)
        {
            Debug.Log($"Used {stack.itemData.itemName} from action bar slot {slotIndex + 1}");

            // TODO: Apply item effects here (heal, buff, etc.)
            // Example: playerHealth.Heal(stack.itemData.healAmount);

            ActionBar.RemoveFromSlot(slotIndex, 1);
        }
        else
        {
            Debug.Log($"Cannot use {stack.itemData.itemName} — it is not consumable.");
        }
    }

    /// <summary>
    /// Moves an item from inventory to the action bar (or vice versa).
    /// </summary>
    public void TransferItem(Inventory source, int sourceIndex, Inventory destination, int destIndex)
    {
        var sourceStack = source.GetSlot(sourceIndex);
        var destStack = destination.GetSlot(destIndex);

        if (sourceStack == null) return;

        // If destination is empty, move the item
        if (destStack == null || !destStack.IsValid)
        {
            destination.SetSlot(destIndex, sourceStack.Clone());
            source.ClearSlot(sourceIndex);
            return;
        }

        // If same item type and stackable, try to merge
        if (sourceStack.itemData == destStack.itemData && sourceStack.itemData.isStackable)
        {
            int canAdd = destStack.RemainingCapacity;
            int toMove = Mathf.Min(sourceStack.quantity, canAdd);

            if (toMove > 0)
            {
                destStack.quantity += toMove;
                sourceStack.quantity -= toMove;

                if (sourceStack.quantity <= 0)
                    source.ClearSlot(sourceIndex);
                else
                    source.SetSlot(sourceIndex, sourceStack); // Trigger update

                destination.SetSlot(destIndex, destStack); // Trigger update
                return;
            }
        }

        // Otherwise swap
        source.SetSlot(sourceIndex, destStack.Clone());
        destination.SetSlot(destIndex, sourceStack.Clone());
    }

    /// <summary>
    /// Whether the inventory panel is currently open.
    /// </summary>
    public bool IsInventoryOpen => isInventoryOpen;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
