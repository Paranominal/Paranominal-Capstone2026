using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Search;
using UnityEngine.UI;

public class RememberSelectedUI : MonoBehaviour
{
    [SerializeField] private EventSystem eventSystem;
    public GameObject lastSelectedElement;
    [SerializeField] private InputActionReference uiNavigateAction;
    [SerializeField] private InputActionReference uiMouseMoveAction;

    private void Reset()
    {
        eventSystem = FindAnyObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            Debug.Log($"[{this}] Did not find Event System in this scene.");
            return;
        }

        lastSelectedElement = eventSystem.firstSelectedGameObject;
    }

    private void Update()
    {
        if (!eventSystem) return;
        
        if (eventSystem.currentSelectedGameObject && lastSelectedElement != eventSystem.currentSelectedGameObject)
        {
            lastSelectedElement = eventSystem.currentSelectedGameObject;
        }

        if (!eventSystem.currentSelectedGameObject &&
        lastSelectedElement &&
        uiNavigateAction != null &&
        uiNavigateAction.action.WasPressedThisFrame())
            eventSystem.SetSelectedGameObject(lastSelectedElement);
        else if (!eventSystem.currentSelectedGameObject &&
        lastSelectedElement &&
        uiNavigateAction == null)
            eventSystem.SetSelectedGameObject(lastSelectedElement);

        if (uiMouseMoveAction.action.ReadValue<Vector2>().magnitude > 0 && eventSystem.currentSelectedGameObject != null)
        {
            eventSystem.currentSelectedGameObject.GetComponent<Button>().OnDeselect(new BaseEventData(eventSystem));
        } 
    }
}
