using UnityEngine;

namespace RelicTrail.Interactions
{
    /// <summary>
    /// Interface for any object or NPC the player can interact with using the [E] key.
    /// </summary>
    public interface IInteractable
    {
        string GetInteractionPrompt();
        void Interact(GameObject interactor);
        bool CanInteract { get; }
    }
}
