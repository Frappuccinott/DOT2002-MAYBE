using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CarIgnition : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private CarStartSystem startSystem;

    [Header("Prompts")]
    [SerializeField] private string startPrompt = "Start Engine [E]";
    [SerializeField] private string stopPrompt = "Stop Engine [E]";

    public string InteractionPrompt => startSystem != null && startSystem.IsRunning ? stopPrompt : startPrompt;
    public InteractionType Type => InteractionType.Interact;
    public bool CanInteract => startSystem != null;

    public void Interact() { }

    public void TriggerIgnition()
    {
        if (startSystem == null) return;

        if (startSystem.IsRunning)
            startSystem.StopEngine();
        else
            startSystem.TryStart();
    }
}
