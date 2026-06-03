using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public bool isPaused; // Flag to track pause state
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject playerUI;
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string grimoireActionMapName = "GrimoireUI";
    [SerializeField] private ALTGrimoire grimoire;

    private void OnEnable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.Disable();
        }
    }


    void Update()
    {
        if (pauseAction != null && pauseAction.action != null && pauseAction.action.WasPressedThisFrame())
        {
            // Toggle pause state on Escape key press
            isPaused = !isPaused;
            if (isPaused)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }
    }

    public void PauseGame()
    {
        // Set Time.timeScale to 0 to pause gameplay
        Time.timeScale = 0;
        InputSystem.actions.FindActionMap(playerActionMapName, true)?.Disable();
        InputSystem.actions.FindActionMap(grimoireActionMapName, true)?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Make PauseMenu panel visible (activate its gameObject)
        playerUI.SetActive(false);
        pauseScreen.SetActive(true);
        
    }

    public void ResumeGame()
    {
        // Set Time.timeScale back to 1 to resume gameplay
        Time.timeScale = 1;
        InputSystem.actions.FindActionMap(playerActionMapName, true)?.Enable();
        InputSystem.actions.FindActionMap(grimoireActionMapName, true)?.Enable();
        if (grimoire != null)
        {
            grimoire.ForceCloseForPause();
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isPaused = false;
        // Hide PauseMenu panel (deactivate its gameObject)
        playerUI.SetActive(true);
        pauseScreen.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}