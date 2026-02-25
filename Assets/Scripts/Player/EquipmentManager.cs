using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Weapon Socket")]
    [SerializeField] private GameObject pickaxeModel;   // drag your pickaxe mesh here

    [Header("Item References")]
    [SerializeField] private ItemData pickaxeItemData;  // drag your Pickaxe ScriptableObject here

    [Header("Animator")]
    [SerializeField] private Animator animator;

    // Animator parameter name — you'll add this in Stage 3
    private static readonly int IsHoldingToolHash = Animator.StringToHash("IsHoldingTool");

    private void OnEnable()
    {
        // Subscribe to action bar selection changes
        ActionBarUI.OnSlotSelected += HandleSlotSelected;
    }

    private void OnDisable()
    {
        ActionBarUI.OnSlotSelected -= HandleSlotSelected;
    }

    private void HandleSlotSelected(ItemData selectedItem)
    {
        bool isPickaxe = selectedItem != null && selectedItem == pickaxeItemData;

        // Show/hide the model
        if (pickaxeModel != null)
            pickaxeModel.SetActive(isPickaxe);

        // Drive animator
        if (animator != null)
            animator.SetBool(IsHoldingToolHash, isPickaxe);
    }
}