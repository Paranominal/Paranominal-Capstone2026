using UnityEngine;

public interface IInteractable
{
    public void Interact();
    GameObject gameObject { get; }
}
