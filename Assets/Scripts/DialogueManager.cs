using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    //[SerializeField] private PauseManager
    // [HideInInspector] public Dialogue dialogue;
    private GameObject dialogue;
    [SerializeField] private GameObject dialogueCanvas;
    public PlayerInputReader playerInputReader;
    public WeaponInputReader weaponInputReader;

    void Start()
    {
        CloseDialogue();
    }
    public void StartDialogue(GameObject pickupDialogue) // public so CollectibleObject can activate it
    {
        UpdateDialogue(pickupDialogue);
        //Time.timeScale = 0f; //pause game
        dialogueCanvas.SetActive(true); //activate UI
        playerInputReader.canMove = false;
        weaponInputReader.canShoot = false;
        SetCursorModeLocked(false); //unlock cursor
    }

    public void NextPage() // public for menu button presses to activate
    {
        //close prev page
        //open next page
        //++page number
    }

    public void CloseDialogue() // public for menu button presses to activate
    {
        SetCursorModeLocked(true); //lock cursor again
        playerInputReader.canMove = true;
        weaponInputReader.canShoot = true;
        dialogueCanvas.gameObject.SetActive(false); //deactivate dialogue
        //Time.timeScale = 1f; //resume game
    }

    public void UpdateDialogue(GameObject pickupDialogue)
    {
        // // do text
        // dialogue.action.text = newDialogue.action.text;
        // dialogue.itemName.text = newDialogue.itemName.text;
        // dialogue.description.text = newDialogue.description.text;
        // dialogue.button.text = newDialogue.button.text;
        // //do item display
        // // Instantiate(newDialogue.itemDisplay, dialogue.itemDisplay.transform.position, dialogue.itemDisplay.transform.rotation);
        // Instantiate(newDialogue.itemDisplay, dialogue.itemDisplay.transform.parent, false);
        // Destroy(dialogue.itemDisplay);
        // dialogue.itemDisplay = newDialogue.itemDisplay;

        GameObject newDialogue = Instantiate(pickupDialogue, dialogueCanvas.transform, false);
        if (dialogue != null) Destroy(dialogue);
        dialogue = newDialogue;

    }

    void SetCursorModeLocked(bool mode) //true for locked, false for unlocked
    {
        if (mode) {
            if (playerInputReader != null) playerInputReader.SetCursorState(CursorLockMode.Locked, false);
            else {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked; } }
        else {
            if (playerInputReader != null) playerInputReader.SetCursorState(CursorLockMode.None, true);
            else {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None; } }
    }
}
