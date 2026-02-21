using UnityEngine;

/// <summary>
/// Place this on world objects that the player can pick up.
/// When the player enters the trigger, the item is added to the inventory.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int quantity = 1;

    [Header("Visual")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float rotateSpeed = 90f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;

        // Ensure collider is set as trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        // Simple floating/bobbing animation
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Slow rotation
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var manager = InventoryManager.Instance;
        if (manager == null) return;

        int overflow = manager.AddItemToInventory(itemData, quantity);

        if (overflow == 0)
        {
            // All items picked up, destroy the pickup
            Debug.Log($"Picked up {quantity}x {itemData.itemName}");
            Destroy(gameObject);
        }
        else if (overflow < quantity)
        {
            // Partial pickup
            quantity = overflow;
            Debug.Log($"Inventory partially full. {overflow}x {itemData.itemName} remaining.");
        }
        else
        {
            Debug.Log("Inventory is full!");
        }
    }
}
