using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    private bool isPaused;
    public bool IsPaused => isPaused;

    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string grimoireActionMapName = "GrimoireUI";
    public void PauseGame()
    {
        // Set Time.timeScale to 0 to pause gameplay
        Time.timeScale = 0;
        InputSystem.actions.FindActionMap(playerActionMapName, true)?.Disable();
        InputSystem.actions.FindActionMap(grimoireActionMapName, true)?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }    
    
    public void ResumeGame()
    {
        // Set Time.timeScale back to 1 to resume gameplay
        Time.timeScale = 1;
        InputSystem.actions.FindActionMap(playerActionMapName, true)?.Enable();
        InputSystem.actions.FindActionMap(grimoireActionMapName, true)?.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isPaused = false;
    }
}
