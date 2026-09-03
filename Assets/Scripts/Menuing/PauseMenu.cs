using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public bool isPaused; // Flag to track pause state
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject debugScreen;
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private GameObject playerUI;
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string grimoireActionMapName = "GrimoireUI";
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private string targetScene;

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
        // Hide all pause menu panels
        playerUI.SetActive(true);
        pauseScreen.SetActive(false);
        if (settingsScreen != null || debugScreen != null)
        {
            settingsScreen.SetActive(false);
            debugScreen.SetActive(false);
        }
    }

    public void OpenSettings()
    {
        if (pauseScreen != null)
        {
            pauseScreen.SetActive(false);
        }
        if (settingsScreen != null)
        {
            settingsScreen.SetActive(true);
        }
    }

    public void OpenDebug()
    {
        if (pauseScreen != null)
        {
            pauseScreen.SetActive(false);
        }
        if (debugScreen != null)
        {
            debugScreen.SetActive(true);
        }
    }

    public void CloseMenu()
    {
        if (settingsScreen != null || debugScreen != null)
        {
            settingsScreen.SetActive(false);
            debugScreen.SetActive(false);
        }
        if (pauseScreen != null)
        {
            pauseScreen.SetActive(true);
        }
    }



    public void QuitGame()
    {
        ResumeGame();
        SceneManager.LoadScene(targetScene);
    }
}