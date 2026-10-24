using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    //[SerializeField] private PauseManager
    //[HideInInspector] public Dialogue dialogue;
    private Dialogue currentDialogue;
    //the current scene's "continue" button 
    [SerializeField] private Button continueButton;
    [SerializeField] private float buttonWaitTime = 1;
    [SerializeField] private InputActionReference closeInput;
    [SerializeField] private Canvas dialogueCanvas;
    public PauseManager pause;
    public PlayerInputReader playerInputReader;
    public WeaponInputReader weaponInputReader;
    [Tooltip("Add this here to open grimoire after hitting continue on an pick-up dialogue!")]
    [SerializeField] GrimoireAnimManager grimoireAnimManager;
    private bool isOpen;
    private bool canEscape = true;

    [SerializeField] private PlayerHUD ui;

    void Awake()
    {
        CloseDialogue();
        if (!pause) Debug.LogWarning($"[{this}] No Pause Manager attached!! this might be a mistake.");
    }
    
    void Update()
    {
        if (!isOpen) return;
        if (closeInput.action.WasPressedThisFrame() && canEscape) CloseDialogue();

        //fallback to press continue directly
        else if (continueButton != null &&
            Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame &&
            canEscape)
        {
            continueButton.onClick.Invoke();
        }
    }

    public void StartDialogue(Dialogue pickupDialogue)
    {
        // if a dialogue is already open, close the previous one, then set the next one and activate canvas
        if (isOpen) CloseDialogue();
        SetDialogue(pickupDialogue);
        isOpen = true;
        dialogueCanvas.gameObject.SetActive(true); //activate UI

        // handle player input, pause, and hide player hud
        if (pause) pause.PauseGame();
        if (playerInputReader != null) playerInputReader.InputLock(true);
        if (weaponInputReader != null) weaponInputReader.InputLock(true);
        if (continueButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        if (ui) ui.UIVisible(false);
        // SetCursorModeLocked(false); //unlock cursor

        // temp disable continue button
        StartCoroutine(DoContinueButtonWait());
    }

    public void NextPage()
    {
        currentDialogue.NextPage();
        StartCoroutine(DoContinueButtonWait());
    }

    public void NextPageOrClose()
    {
        if (currentDialogue.CurrentPage < currentDialogue.PageCount)
        {
            NextPage();
        }
        else CloseDialogue();
    }

    public void CloseDialogue(bool openGrimoire = false)
    {
        // if (currentDialogue != null) SetCursorModeLocked(currentDialogue.GetComponent<Dialogue>().cursorLockOnClose); //lock cursor again
        if (playerInputReader != null) playerInputReader.InputLock(false);
        if (weaponInputReader != null) weaponInputReader.InputLock(false);
        dialogueCanvas.gameObject.SetActive(false); //deactivate dialogue
        if (grimoireAnimManager != null && openGrimoire) grimoireAnimManager.OpenFromDialogue(); //open grimoire
        if (pause) pause.ResumeGame();
        isOpen = false;

        if (ui) ui.UIVisible(true);
    }

    public void SetDialogue(Dialogue pickupDialogue)
    {
        // instantiate fresh dialogue
        Dialogue newDialogue = Instantiate(pickupDialogue, dialogueCanvas.transform, false);

        // remove old dialogue
        if (currentDialogue != null) Destroy(currentDialogue.gameObject);

        //  first page opens automatically

        // replace old dialogue
        currentDialogue = newDialogue;
    }
    
    private IEnumerator DoContinueButtonWait()
    {
        continueButton.gameObject.SetActive(false);
        canEscape = false;
        yield return new WaitForSecondsRealtime(buttonWaitTime);
        canEscape = true;
        continueButton.gameObject.SetActive(true);
    }
}