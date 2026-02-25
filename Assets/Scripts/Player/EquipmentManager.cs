using UnityEngine;

/// <summary>
/// Manages equipping and unequipping items by instantiating their prefabs
/// onto the WeaponSocket bone at runtime. Subscribes to ActionBarUI.OnSlotSelected.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [Header("Sockets")]
    [SerializeField] private Transform weaponSocket; // drag WeaponSocket transform here

    [Header("Animator")]
    [SerializeField] private Animator animator;

    // Animator parameter hashes
    private static readonly int IsHoldingToolHash = Animator.StringToHash("IsHoldingTool");
    private static readonly int IsHoldingWeaponHash = Animator.StringToHash("IsHoldingWeapon");

    // The currently instantiated equipment model
    private GameObject _currentEquippedModel;

    // Static property so other systems (MiningNode etc.) can query what's equipped
    public static ItemData CurrentlyEquippedItem { get; private set; }

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void OnEnable()
    {
        ActionBarUI.OnSlotSelected += HandleSlotSelected;
    }

    private void OnDisable()
    {
        ActionBarUI.OnSlotSelected -= HandleSlotSelected;
    }

    // ─────────────────────────────────────────────
    //  Equipment Logic
    // ─────────────────────────────────────calls
    // ─────────────────────────────────────────────

    private void HandleSlotSelected(ItemData selectedItem)
    {
        // Unequip the current item first
        UnequipCurrent();

        CurrentlyEquippedItem = selectedItem;

        if (selectedItem == null) return;
        if (selectedItem.equippablePrefab == null) return; // item has no 3D model to show

        // Instantiate the new model on the weapon socket
        _currentEquippedModel = Instantiate(
            selectedItem.equippablePrefab,
            weaponSocket.position,
            weaponSocket.rotation,
            weaponSocket  // parent it to the socket so it follows the hand bone
        );

        // Zero out local transform so it sits exactly where you placed it in the prefab
        _currentEquippedModel.transform.localPosition = Vector3.zero;
        _currentEquippedModel.transform.localRotation = Quaternion.identity;
        _currentEquippedModel.transform.localScale = Vector3.one;

        // Drive animator based on item category
        UpdateAnimatorForItem(selectedItem);
    }

    private void UnequipCurrent()
    {
        if (_currentEquippedModel != null)
        {
            Destroy(_currentEquippedModel);
            _currentEquippedModel = null;
        }

        // Reset all equipment-related animator bools
        if (animator != null)
        {
            animator.SetBool(IsHoldingToolHash, false);
            animator.SetBool(IsHoldingWeaponHash, false);
        }
    }

    private void UpdateAnimatorForItem(ItemData item)
    {
        if (animator == null || item == null) return;

        switch (item.category)
        {
            case ItemCategory.Tool:
                animator.SetBool(IsHoldingToolHash, true);
                break;
            case ItemCategory.Weapon:
                animator.SetBool(IsHoldingWeaponHash, true);
                break;
                // Add more cases as you add new item categories
        }
    }
}