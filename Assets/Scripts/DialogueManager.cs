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
    private GameObject rememberedElement;
    //the current scene's "continue" button 
    [SerializeField] private Button continueButton;
    // [SerializeField] private float buttonWaitTime = 1;
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

        // //fallback to press continue directly
        // else if (continueButton != null &&
        //     Gamepad.current != null &&
        //     Gamepad.current.buttonSouth.wasPressedThisFrame &&
        //     canEscape)
        // {
        //     continueButton.onClick.Invoke();
        // }
        if (closeInput.action.WasPressedThisFrame()) CloseDialogue();
        
        //removed fallback, using continue button gameobject selection instead
        // else if (continueButton != null && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) continueButton.onClick.Invoke();
    }

    public void StartDialogue(Dialogue pickupDialogue)
    {
        // if a dialogue is already open close it
        if (isOpen) CloseDialogue();
        // remember the previous selected element to return to later
        if (EventSystem.current != null) rememberedElement = EventSystem.current.currentSelectedGameObject;
        // set the new dialogue
        SetDialogue(pickupDialogue);
        // activates the dialogue UI
        dialogueCanvas.gameObject.SetActive(true); 
        // handle state, player input, pause, and hud
        if (playerInputReader != null) playerInputReader.InputLock(true);
        if (weaponInputReader != null) weaponInputReader.InputLock(true);
        if (pause) pause.PauseGame();
        if (ui) ui.UIVisible(false);
        isOpen = true;
        // clear and reset the continue button
        if (continueButton != null && EventSystem.current != null) // clear and reset continue button
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
        // SetCursorModeLocked(false); //unlock cursor

        // temp disable continue button
        StartCoroutine(DoContinueButtonWait(pickupDialogue.WaitTime));
    }

    public void NextPage()
    {
        currentDialogue.NextPage();
        StartCoroutine(DoContinueButtonWait(currentDialogue.WaitTime));
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
        bool wasOpen = isOpen; //check to see if a previous button can be returned
        if (wasOpen) PreviousButtonClicked();

        // if (dialogueObject != null) SetCursorModeLocked(dialogueObject.GetComponent<Dialogue>().cursorLockOnClose); //lock cursor again
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
    
    private IEnumerator DoContinueButtonWait(float wait = 1f)
    {
        continueButton.gameObject.SetActive(false);
        canEscape = false;
        if (wait > 0) yield return new WaitForSecondsRealtime(wait);
        canEscape = true;
        continueButton.gameObject.SetActive(true);
    }

    private void PreviousButtonClicked()
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);

        //return previous button if it exists
        if (rememberedElement != null && rememberedElement.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(rememberedElement);
        else if (EventSystem.current.firstSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(EventSystem.current.firstSelectedGameObject);

        //clear previous reference after operation
        rememberedElement = null;
    }
}