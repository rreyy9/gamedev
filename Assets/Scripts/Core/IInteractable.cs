using UnityEngine;

/// <summary>
/// Interface for all objects the player can interact with.
/// Implemented by Lootable, and future types like dialogue NPCs, doors, levers.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Display name shown in the interaction prompt (e.g., "Loot Goblin", "Open Chest").
    /// </summary>
    string InteractionPrompt { get; }

    /// <summary>
    /// Whether the player can currently interact with this object.
    /// </summary>
    bool CanInteract { get; }

    /// <summary>
    /// The transform of the interactable (for distance checks).
    /// </summary>
    Transform InteractableTransform { get; }

    /// <summary>
    /// Called when the player presses the interact key.
    /// </summary>
    void Interact();

    /// <summary>
    /// Shows or hides the interaction highlight on this object.
    /// </summary>
    void SetHighlight(bool active);
}