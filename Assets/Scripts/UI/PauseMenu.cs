using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public bool isPaused;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject debugScreen;
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private PlayerHUD playerHUD;
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string grimoireActionMapName = "GrimoireUI";
    [SerializeField] private ALTGrimoire grimoire;

    // EDIT (auto-resolve): fallback for cross-prefab references.
    private void Awake()
    {
        if (playerHUD == null)
            playerHUD = FindAnyObjectByType<PlayerHUD>();
        if (grimoire == null)
            grimoire = FindAnyObjectByType<ALTGrimoire>();
    }

    private void OnEnable()
    {
        if (pauseAction != null && pauseAction.action != null)
            pauseAction.action.Enable();
    }

    private void OnDisable()
    {
        if (pauseAction != null && pauseAction.action != null)
            pauseAction.action.Disable();
    }

    void Update()
    {
        if (pauseAction != null && pauseAction.action != null && pauseAction.action.WasPressedThisFrame())
        {
            isPaused = !isPaused;
            if (isPaused)
                PauseGame();
            else
                ResumeGame();
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        InputSystem.actions.FindActionMap(playerActionMapName, true)?.Disable();
        InputSystem.actions.FindActionMap(grimoireActionMapName, true)?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerHUD != null) playerHUD.SetHUDActive(false);

        pauseScreen.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        InputSystem.actions.FindActionMap(playerActionMapName, true)?.Enable();
        InputSystem.actions.FindActionMap(grimoireActionMapName, true)?.Enable();
        if (grimoire != null)
            grimoire.ForceCloseForPause();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isPaused = false;

        if (playerHUD != null)
        {
            playerHUD.SetHUDActive(true);
            playerHUD.UIVisible(true);
        }

        pauseScreen.SetActive(false);
        if (settingsScreen != null || debugScreen != null)
        {
            settingsScreen.SetActive(false);
            debugScreen.SetActive(false);
        }
    }

    public void OpenSettings()
    {
        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (settingsScreen != null) settingsScreen.SetActive(true);
    }

    public void OpenDebug()
    {
        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (debugScreen != null) debugScreen.SetActive(true);
    }

    public void CloseMenu()
    {
        if (settingsScreen != null || debugScreen != null)
        {
            settingsScreen.SetActive(false);
            debugScreen.SetActive(false);
        }
        if (pauseScreen != null) pauseScreen.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
