using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Individual loot row in the loot window.
/// Displays an item icon, name, and quantity.
/// Clicking the slot loots that item into the player's inventory.
/// </summary>
public class LootSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image highlightImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.3f, 0.15f, 0.9f);
    [SerializeField] private Color errorFlashColor = new Color(0.5f, 0.1f, 0.1f, 0.9f);

    // Runtime
    private int slotIndex;
    private LootSource sourceLootSource;
    private ItemStack itemStack;

    /// <summary>
    /// Configures this slot to display a specific loot entry.
    /// </summary>
    public void Setup(int index, ItemStack stack, LootSource source)
    {
        slotIndex = index;
        itemStack = stack;
        sourceLootSource = source;

        if (stack == null || stack.itemData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = stack.itemData.icon;
            iconImage.enabled = stack.itemData.icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = stack.itemData.itemName;
        }

        if (quantityText != null)
        {
            quantityText.text = stack.quantity > 1 ? stack.quantity.ToString() : "";
        }

        if (highlightImage != null)
            highlightImage.enabled = false;

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (sourceLootSource == null) return;
        if (itemStack == null || itemStack.itemData == null) return;

        int overflow = sourceLootSource.TakeLootItem(slotIndex);

        if (overflow == 0)
        {
            if (LootUIManager.Instance != null)
                LootUIManager.Instance.RefreshLootWindow();
        }
        else if (overflow > 0)
        {
            if (quantityText != null)
                quantityText.text = overflow > 1 ? overflow.ToString() : "";

            if (backgroundImage != null)
                StartCoroutine(FlashColor());

            Debug.Log($"Inventory full! {overflow}x {itemStack.itemData.itemName} couldn't be picked up.");
        }
        else if (overflow == -1)
        {
            if (LootUIManager.Instance != null)
                LootUIManager.Instance.RefreshLootWindow();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = true;

        if (backgroundImage != null)
            backgroundImage.color = hoverColor;

        if (itemStack != null && itemStack.itemData != null && TooltipUI.Instance != null)
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            TooltipUI.Instance.Show(itemStack.itemData, mousePos);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = false;

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }

    private IEnumerator FlashColor()
    {
        if (backgroundImage == null) yield break;

        backgroundImage.color = errorFlashColor;
        yield return new WaitForSeconds(0.3f);
        backgroundImage.color = normalColor;
    }
}