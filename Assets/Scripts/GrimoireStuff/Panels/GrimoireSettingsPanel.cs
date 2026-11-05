using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Summary: Settings tab of the Full Interface. Shows the settings page on BookL, with Quit to Menu and a Debug button.
// Debug toggles the debug options page on BookR. The settings widgets themselves (sensitivity, FOV, etc.) live on the page unchanged.
public class GrimoireSettingsPanel : MonoBehaviour
{
    [Header("Pages")]
    [Tooltip("Settings options on BookL. Should be a child of the Full Interface BookL container.")]
    [SerializeField] private GameObject settingsPage;
    [Tooltip("Debug options on BookR. Should be a child of the Full Interface BookR container.")]
    [SerializeField] private GameObject debugPage;

    [Header("Buttons")]
    [SerializeField] private Button debugButton;
    [SerializeField] private Button quitButton;

    [Header("Quit to Menu")]
    [SerializeField] private int menuSceneBuildIndex;

    [Header("External Systems")]
    [SerializeField] private ALTGrimoire grimoire;

    private void Awake()
    {
        if (debugButton != null)
            debugButton.onClick.AddListener(ToggleDebug);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitToMenu);
    }

    private void OnEnable()
    {
        if (settingsPage != null) settingsPage.SetActive(true);
        if (debugPage != null) debugPage.SetActive(false);
    }

    private void OnDisable()
    {
        if (settingsPage != null) settingsPage.SetActive(false);
        if (debugPage != null) debugPage.SetActive(false);
    }

    private void ToggleDebug()
    {
        if (debugPage != null)
            debugPage.SetActive(!debugPage.activeSelf);
    }

    // Summary: Closes the Grimoire first so time and input are restored before the scene changes.
    private void QuitToMenu()
    {
        if (grimoire == null)
            grimoire = ALTGrimoire.instance != null ? ALTGrimoire.instance : FindAnyObjectByType<ALTGrimoire>();

        if (grimoire != null)
            grimoire.CloseGrimoire();

        SceneManager.LoadScene(menuSceneBuildIndex);
    }
}
