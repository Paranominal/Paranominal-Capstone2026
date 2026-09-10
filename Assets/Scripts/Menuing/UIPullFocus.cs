using UnityEngine;
using UnityEngine.EventSystems;

public class UIPullFocus : MonoBehaviour
{
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private GameObject newFocus;
    [SerializeField] private DialogueManager dialogueManager;   //i need to work out the code to detect which dialogue is being used in order to select the correct button to return to. alas.

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (eventSystem == null)
        {
            eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("No event system in the scene. Did you mean to include this script?");
            }
        }

        if (dialogueManager == null)
        {
            dialogueManager = FindAnyObjectByType<DialogueManager>();
        }

    }

    public void PullFocus()
    {
        eventSystem.SetSelectedGameObject(newFocus);
        Debug.Log(eventSystem.currentSelectedGameObject);
    }
}
