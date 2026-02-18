using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// UI representation of a single inventory slot.
/// Handles display, click interactions, and drag-and-drop initiation.
/// Attach to each slot prefab.
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler,
    IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Image slotBackground;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color highlightColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.5f, 0.3f, 0.9f);

    // Data
    private Inventory inventory;
    private int slotIndex;
    private bool isActionBarSlot;

    // Drag state (static so it's shared across all slots)
    private static InventorySlotUI dragSource;
    private static GameObject dragIcon;
    private static Canvas rootCanvas;

    // ─────────────────────────────────────────────
    //  Helper: Check if Shift is held (New Input System)
    // ─────────────────────────────────────────────

    private bool IsShiftHeld()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;
        return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
    }

    // ─────────────────────────────────────────────
    //  Initialization
    // ─────────────────────────────────────────────

    /// <summary>
    /// Sets up this slot with its backing inventory and index.
    /// </summary>
    public void Setup(Inventory inventory, int index, bool isActionBar = false)
    {
        this.inventory = inventory;
        this.slotIndex = index;
        this.isActionBarSlot = isActionBar;

        // Cache canvas reference for drag operations
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        Refresh();
    }

    /// <summary>
    /// Updates the visual display to match the current data.
    /// </summary>
    public void Refresh()
    {
        var stack = inventory?.GetSlot(slotIndex);

        if (stack != null && stack.IsValid)
        {
            // Show item
            iconImage.sprite = stack.itemData.icon;
            iconImage.color = Color.white;
            iconImage.enabled = true;

            // Show quantity for stacks > 1
            if (stack.quantity > 1)
            {
                quantityText.text = stack.quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
        else
        {
            // Empty slot
            iconImage.sprite = null;
            iconImage.color = Color.clear;
            iconImage.enabled = false;
            quantityText.enabled = false;
        }
    }

    // ─────────────────────────────────────────────
    //  Click Handling
    // ─────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventory == null) return;

        // Right-click: split stack or use item
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // If shift is held, attempt to split
            if (IsShiftHeld())
            {
                TrySplitToFirstEmpty();
            }
            else
            {
                // Right-click uses item if on action bar
                TryUseItem();
            }
        }
    }

    private void TrySplitToFirstEmpty()
    {
        // Find first empty slot in the same inventory
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            if (i != slotIndex && inventory.IsSlotEmpty(i))
            {
                inventory.SplitStack(slotIndex, i);
                return;
            }
        }
    }

    private void TryUseItem()
    {
        var stack = inventory.GetSlot(slotIndex);
        if (stack == null || !stack.IsValid) return;

        if (stack.itemData.isConsumable && isActionBarSlot)
        {
            InventoryManager.Instance?.UseActionBarSlot(slotIndex);
        }
    }

    // ─────────────────────────────────────────────
    //  Drag and Drop
    // ─────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        var stack = inventory?.GetSlot(slotIndex);
        if (stack == null || !stack.IsValid) return;

        dragSource = this;

        // Create a floating icon that follows the cursor
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(rootCanvas.transform, false);

        var img = dragIcon.AddComponent<Image>();
        img.sprite = stack.itemData.icon;
        img.raycastTarget = false;
        img.SetNativeSize();

        // Scale the drag icon
        var rectTransform = dragIcon.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(40f, 40f);

        // Make the source slot look dimmed
        if (iconImage != null)
            iconImage.color = new Color(1f, 1f, 1f, 0.3f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;

        // Follow mouse position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            rootCanvas.worldCamera,
            out Vector2 localPoint);

        dragIcon.transform.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Clean up drag icon
        if (dragIcon != null)
            Destroy(dragIcon);

        dragIcon = null;
        dragSource = null;

        // Restore slot visual
        Refresh();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (dragSource == null || dragSource == this) return;

        // Determine if this is a cross-inventory transfer or same-inventory swap
        if (dragSource.inventory == this.inventory)
        {
            // Same inventory: swap/merge
            if (IsShiftHeld())
            {
                // Shift-drag: split half to target
                inventory.SplitStack(dragSource.slotIndex, this.slotIndex);
            }
            else
            {
                inventory.SwapSlots(dragSource.slotIndex, this.slotIndex);
            }
        }
        else
        {
            // Cross-inventory transfer (inventory <-> action bar)
            InventoryManager.Instance?.TransferItem(
                dragSource.inventory, dragSource.slotIndex,
                this.inventory, this.slotIndex);
        }

        // Refresh both slots
        dragSource.Refresh();
        Refresh();
    }

    // ─────────────────────────────────────────────
    //  Hover Highlight
    // ─────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotBackground != null)
            slotBackground.color = highlightColor;

        // Show tooltip
        var stack = inventory?.GetSlot(slotIndex);
        if (stack != null && stack.IsValid)
        {
            TooltipUI.Instance?.Show(stack.itemData, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotBackground != null)
            slotBackground.color = normalColor;

        TooltipUI.Instance?.Hide();
    }

    // ─────────────────────────────────────────────
    //  Action Bar Selection Highlight
    // ─────────────────────────────────────────────

    /// <summary>
    /// Highlights this slot as the currently selected action bar slot.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (highlightImage != null)
        {
            highlightImage.enabled = selected;
        }

        if (slotBackground != null)
        {
            slotBackground.color = selected ? selectedColor : normalColor;
        }
    }
}