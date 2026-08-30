using UnityEngine;
using UnityEngine.EventSystems;

public class UIPullFocus : MonoBehaviour
{
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private GameObject newFocus;

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

    }

    public void PullFocus()
    {
        eventSystem.SetSelectedGameObject(newFocus);
        //Debug.Log(eventSystem.currentSelectedGameObject);
    }
}
