using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    private bool isPaused;
    public bool IsPaused => isPaused;

    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string grimoireActionMapName = "GrimoireUI";
    [SerializeField] private PlayerInputReader playerInputReader;
    public void PauseGame()
    {
        // Set Time.timeScale to 0 to pause gameplay
        Time.timeScale = 0;
        InputSystem.actions.FindActionMap(playerActionMapName, true)?.Disable();
        InputSystem.actions.FindActionMap(grimoireActionMapName, true)?.Disable();
        SetCursorModeLocked(false);
        Cursor.visible = true;
    }    
    
    public void ResumeGame()
    {
        // Set Time.timeScale back to 1 to resume gameplay
        Time.timeScale = 1;
        InputSystem.actions.FindActionMap(playerActionMapName, true)?.Enable();
        InputSystem.actions.FindActionMap(grimoireActionMapName, true)?.Enable();
        SetCursorModeLocked(true);
        Cursor.visible = false;
        isPaused = false;
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
