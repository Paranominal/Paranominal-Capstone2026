using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    //[SerializeField] private PauseManager
    //[HideInInspector] public Dialogue dialogue;
    private GameObject dialogueObject;
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
        if (playerInputReader != null) playerInputReader.canMove = false;
        if (weaponInputReader != null) weaponInputReader.canShoot = false;
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
        if (dialogueObject != null) SetCursorModeLocked(dialogueObject.GetComponent<Dialogue>().cursorLockOnClose); //lock cursor again
        if (playerInputReader != null) playerInputReader.canMove = true;
        if (weaponInputReader != null) weaponInputReader.canShoot = true;
        dialogueCanvas.gameObject.SetActive(false); //deactivate dialogue
        // Time.timeScale = 1f; //resume game
    }

    public void UpdateDialogue(GameObject pickupDialogue)
    {
        GameObject newDialogue = Instantiate(pickupDialogue, dialogueCanvas.transform, false);
        if (dialogueObject != null) Destroy(dialogueObject);
        dialogueObject = newDialogue;
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
