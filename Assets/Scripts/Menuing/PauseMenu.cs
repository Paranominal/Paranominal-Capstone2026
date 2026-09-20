using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject debugScreen;
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private GameObject playerUI;
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private int sceneBuildIndex;

    private void OnEnable()
    {
        // resolve cross-prefab references
        if (grimoire == null)
            grimoire = FindFirstObjectByType<ALTGrimoire>();
        if (playerUI == null)
        {
            GameObject uiObj = GameObject.Find("UI");
            if (uiObj != null) playerUI = uiObj;
        }

        UIResumeGame();

        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        UIResumeGame();

        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.Disable();
        }
    }

    private bool isPauseMenuOpen;

    void Update()
    {

        if (pauseAction != null && pauseAction.action != null && pauseAction.action.WasPressedThisFrame())
        {
            // Toggle pause state on Escape key press
            if (!isPauseMenuOpen)
            {
                UIPauseGame();
            }
            else
            {
                UIResumeGame();
            }
        }
    }

    public void UIPauseGame()
    {
        pauseManager.PauseGame();
        isPauseMenuOpen = true;
        // Make PauseMenu panel visible (activate its gameObject)
        playerUI.SetActive(false);
        pauseScreen.SetActive(true);
        
    }

    public void UIResumeGame()
    {
        pauseManager.ResumeGame();
        isPauseMenuOpen = false;
        if (grimoire != null)
        {
            grimoire.ForceCloseForPause();
        }
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
        UIResumeGame();
        SceneManager.LoadScene(sceneBuildIndex);
    }
}