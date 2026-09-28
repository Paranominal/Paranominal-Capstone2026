using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    //[SerializeField] private PauseManager
    //[HideInInspector] public Dialogue dialogue;
    private GameObject dialogueObject;
    [SerializeField] private UIPullFocus pullFocus;
    [SerializeField] private GameObject continueButton;
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
        if (isOpen && closeInput.action.WasPressedThisFrame()) CloseDialogue();
        if (isOpen) HoldFocusOnContinue();
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
        //auto lock continue button
        if (continueButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(continueButton);
    }

    private void HoldFocusOnContinue()
    {
        if (continueButton == null || EventSystem.current == null) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(dialogueCanvas.transform)) return;
        EventSystem.current.SetSelectedGameObject(continueButton);
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
        if (grimoireAnimManager != null && openGrimoire) grimoireAnimManager.OpenFromDialogue(); //open grimoire
        if (pause) pause.ResumeGame();
        isOpen = false;
        if (pullFocus != null && !openGrimoire) pullFocus.PullFocus();
    }

    public void UpdateDialogue(GameObject pickupDialogue)
    {
        GameObject newDialogue = Instantiate(pickupDialogue, dialogueCanvas.transform, false);
        if (dialogueObject != null) Destroy(dialogueObject);
        dialogueObject = newDialogue;
    }
}
