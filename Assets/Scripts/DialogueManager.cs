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
        //fallback to press continue directly
        else if (continueButton != null && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) continueButton.onClick.Invoke();
    }
    public void StartDialogue(GameObject pickupDialogue) // public so CollectibleObject can activate it
    {
        if (isOpen) CloseDialogue();
        UpdateDialogue(pickupDialogue);
        dialogueCanvas.SetActive(true); //activate UI
        if (playerInputReader != null) playerInputReader.InputLock(true);
        if (weaponInputReader != null) weaponInputReader.InputLock(true);
        // SetCursorModeLocked(false); //unlock cursor
        if (pause) pause.PauseGame();
        isOpen = true;
        if (continueButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
    }

    public void NextPage() // public for menu button presses to activate
    {
        //close prev page
        //open next page
        //++page number
    }

    public void CloseDialogue(bool openGrimoire = false) // public for menu button presses to activate
    {
        // if (dialogueObject != null) SetCursorModeLocked(dialogueObject.GetComponent<Dialogue>().cursorLockOnClose); //lock cursor again
        if (playerInputReader != null) playerInputReader.InputLock(false);
        if (weaponInputReader != null) weaponInputReader.InputLock(false);
        dialogueCanvas.gameObject.SetActive(false); //deactivate dialogue
        // if (grimoireAnimManager != null && openGrimoire) grimoireAnimManager.OpenFromDialogue(); //open grimoire
        if (pause) pause.ResumeGame();
        isOpen = false;
    }

    public void UpdateDialogue(GameObject pickupDialogue)
    {
        GameObject newDialogue = Instantiate(pickupDialogue, dialogueCanvas.transform, false);
        if (dialogueObject != null) Destroy(dialogueObject);
        dialogueObject = newDialogue;
    }
}