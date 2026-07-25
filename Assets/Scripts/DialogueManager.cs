using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    //[SerializeField] private PauseManager
    [HideInInspector]public Dialogue dialogue;
    [SerializeField] private GameObject dialogueCanvas;
    public PlayerInputReader playerInputReader;
    public WeaponInputReader weaponInputReader;

    void Start()
    {
        CloseDialogue();
    }
    public void StartDialogue() // public so CollectibleObject can activate it
    {
        //Time.timeScale = 0f; //pause game
        dialogueCanvas.gameObject.SetActive(true); //activate UI
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
