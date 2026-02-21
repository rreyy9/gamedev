using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Simple tooltip that shows item name, category, and description on hover.
/// Attach to a UI panel that starts disabled.
/// </summary>
public class TooltipUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RectTransform tooltipRect;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(10f, -10f);

    public static TooltipUI Instance { get; private set; }

    private Canvas rootCanvas;
    private RectTransform canvasRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        rootCanvas = GetComponentInParent<Canvas>();
        canvasRect = rootCanvas.GetComponent<RectTransform>();

        if (tooltipRect == null)
            tooltipRect = GetComponent<RectTransform>();

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows the tooltip for the given item near the given screen position.
    /// </summary>
    public void Show(ItemData item, Vector3 screenPosition)
    {
        if (item == null) return;

        itemNameText.text = item.itemName;
        categoryText.text = item.category.ToString();
        descriptionText.text = item.description;

        // Add consumable info
        if (item.isConsumable)
        {
            descriptionText.text += "\n<color=#00FF88>Right-click to use</color>";
        }

        // Add stack info
        if (item.isStackable)
        {
            descriptionText.text += $"\n<color=#AAAAAA>Max Stack: {item.maxStackSize}</color>";
        }

        gameObject.SetActive(true);

        // Position tooltip near cursor, clamped to screen
        UpdatePosition(screenPosition);
    }

    /// <summary>
    /// Hides the tooltip.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // Follow mouse while visible using New Input System
        if (gameObject.activeSelf)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                UpdatePosition(mouse.position.ReadValue());
            }
        }
    }

    private void UpdatePosition(Vector3 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, rootCanvas.worldCamera, out Vector2 localPoint);

        localPoint += offset;

        // Clamp to canvas bounds
        Vector2 tooltipSize = tooltipRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        float maxX = canvasSize.x / 2f - tooltipSize.x;
        float minY = -canvasSize.y / 2f + tooltipSize.y;

        localPoint.x = Mathf.Min(localPoint.x, maxX);
        localPoint.y = Mathf.Max(localPoint.y, minY);

        tooltipRect.localPosition = localPoint;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}