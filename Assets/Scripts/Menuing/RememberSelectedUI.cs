using UnityEditor.Search;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Search;
using UnityEngine.UI;

public class RememberSelectedUI : MonoBehaviour
{
    [SerializeField] private EventSystem eventSystem;
    private GameObject lastSelectedElement;
    // [SerializeField] private float selectionTimeoutSeconds = 4f;
    // [SerializeField] private bool doSelectionTimeout = true;
    [SerializeField] private InputActionReference uiNavigateAction;
    // private bool timedOut;
    // private void Start()
    // {
    //     currentTimeoutTime = selectionTimeoutSeconds;
    //     // if (eventSystem != null && eventSystem.currentSelectedGameObject != null) lastSelectedElement = eventSystem.currentSelectedGameObject;
    // }

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
        // if (!timedOut && doSelectionTimeout) Timeout();
        
        if (eventSystem.currentSelectedGameObject && lastSelectedElement != eventSystem.currentSelectedGameObject)
        {
            lastSelectedElement = eventSystem.currentSelectedGameObject;
            // currentTimeoutTime = selectionTimeoutSeconds;
            // timedOut = false;
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
    }
    // private float currentTimeoutTime;
    // private Button cacheDeselectButton;
    // private void Timeout()
    // {
    //     if (currentTimeoutTime > 0)
    //     {
    //         currentTimeoutTime -= Time.deltaTime;
    //         return;
    //     }
    //     cacheDeselectButton = eventSystem.currentSelectedGameObject.GetComponent<Button>();
    //     eventSystem.SetSelectedGameObject(null);
    //     cacheDeselectButton.OnDeselect(new PointerEventData(EventSystem.current));
    //     Button.curr
    //     // cacheDeselectButton.enabled = false;
    //     // cacheDeselectButton.enabled = true; //hacky way to reset the selection highlight
    //     // timedOut = true;
    // }
    // // public void OnPointerExit(PointerEventData eventData)
    // // {
    //     cacheDeselectButton.OnDeselect(eventData);
    // }
}
