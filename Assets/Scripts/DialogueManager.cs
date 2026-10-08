using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    //[SerializeField] private PauseManager
    //[HideInInspector] public Dialogue dialogue;
    private GameObject dialogueObject;
    private GameObject previousButton;
    //the current scene's "continue" button 
    [SerializeField] private Button continueButton;
    [SerializeField] private InputActionReference closeInput;
    [SerializeField] private GameObject dialogueCanvas;
    public PauseManager pause;
    public PlayerInputReader playerInputReader;
    public WeaponInputReader weaponInputReader;
    [Tooltip("Add this here to open grimoire after hitting continue on an pick-up dialogue!")]
    [SerializeField] GrimoireAnimManager grimoireAnimManager;
    private bool isOpen;

    void Start()
    {
        CloseDialogue();
        if (!pause) Debug.LogWarning($"[{this}] No Pause Manager attached!! this might be a mistake.");
    }
    void Update()
    {
        if (!isOpen) return;
        if (closeInput.action.WasPressedThisFrame()) CloseDialogue();
        
        //removed fallback, using continue button gameobject selection instead
        // else if (continueButton != null && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) continueButton.onClick.Invoke();
    }
    public void StartDialogue(GameObject pickupDialogue) // public so CollectibleObject can activate it
    {
        if (isOpen) CloseDialogue();
        if (EventSystem.current != null) previousButton = EventSystem.current.currentSelectedGameObject; //selects the button that opened the dialogue
        UpdateDialogue(pickupDialogue);
        dialogueCanvas.SetActive(true); //activate UI
        if (playerInputReader != null) playerInputReader.InputLock(true);
        if (weaponInputReader != null) weaponInputReader.InputLock(true);
        // SetCursorModeLocked(false); //unlock cursor
        if (pause) pause.PauseGame();
        isOpen = true;
        if (continueButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
    }

    public void NextPage() // public for menu button presses to activate
    {
        //close prev page
        //open next page
        //++page number
    }

    public void CloseDialogue(bool openGrimoire = false) // public for menu button presses to activate
    {
        bool wasOpen = isOpen; //check to see if a previous button can be returned
        if (wasOpen) PreviousButtonClicked();

        // if (dialogueObject != null) SetCursorModeLocked(dialogueObject.GetComponent<Dialogue>().cursorLockOnClose); //lock cursor again
        if (playerInputReader != null) playerInputReader.InputLock(false);
        if (weaponInputReader != null) weaponInputReader.InputLock(false);
        dialogueCanvas.gameObject.SetActive(false); //deactivate dialogue
        if (grimoireAnimManager != null && openGrimoire) grimoireAnimManager.OpenFromDialogue(); //open grimoire
        if (pause) pause.ResumeGame();
        isOpen = false;
    }

    public void UpdateDialogue(GameObject pickupDialogue)
    {
        GameObject newDialogue = Instantiate(pickupDialogue, dialogueCanvas.transform, false);
        if (dialogueObject != null) Destroy(dialogueObject);
        dialogueObject = newDialogue;
    }

    private void PreviousButtonClicked()
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);

        //return previous button if it exists
        if (previousButton != null && previousButton.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousButton);
        else if (EventSystem.current.firstSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(EventSystem.current.firstSelectedGameObject);

        //clear previous reference after operation
        previousButton = null;
    }
}