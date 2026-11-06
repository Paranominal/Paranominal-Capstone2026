using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Summary: Settings tab of the Full Interface. Shows the settings page on BookL, with Quit to Menu and a Debug button.
// Debug toggles the debug options page on BookR. The settings widgets themselves (sensitivity, FOV, etc.) live on the page unchanged.
// EDIT (graphics-settings): Graphics button toggles the Graphics settings page on BookR. Only one BookR page shows at a time.
public class GrimoireSettingsPanel : MonoBehaviour
{
    [Header("Pages")]
    [Tooltip("Settings options on BookL. Should be a child of the Full Interface BookL container.")]
    [SerializeField] private GameObject settingsPage;
    [Tooltip("Debug options on BookR. Should be a child of the Full Interface BookR container.")]
    [SerializeField] private GameObject debugPage;
    // EDIT (graphics-settings): Graphics settings page.
    [Tooltip("Graphics settings on BookR. Should be a child of the Full Interface BookR container.")]
    [SerializeField] private GameObject graphicsPage;

    [Header("Buttons")]
    [SerializeField] private Button debugButton;
    [SerializeField] private Button graphicsButton; // EDIT (graphics-settings)
    [SerializeField] private Button quitButton;

    [Header("Quit to Menu")]
    [SerializeField] private int menuSceneBuildIndex;

    [Header("External Systems")]
    [SerializeField] private ALTGrimoire grimoire;

    private void Awake()
    {
        if (debugButton != null)
            debugButton.onClick.AddListener(ToggleDebug);
        if (graphicsButton != null)
            graphicsButton.onClick.AddListener(ToggleGraphics); // EDIT (graphics-settings)
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitToMenu);
    }

    private void OnEnable()
    {
        if (settingsPage != null) settingsPage.SetActive(true);
        if (debugPage != null) debugPage.SetActive(false);
        if (graphicsPage != null) graphicsPage.SetActive(false); // EDIT (graphics-settings)
    }

    private void OnDisable()
    {
        if (settingsPage != null) settingsPage.SetActive(false);
        if (debugPage != null) debugPage.SetActive(false);
        if (graphicsPage != null) graphicsPage.SetActive(false); // EDIT (graphics-settings)
    }

    // EDIT (graphics-settings): now goes through ToggleRightPage so Graphics closes when Debug opens.
    private void ToggleDebug()
    {
        ToggleRightPage(debugPage);
    }

    // EDIT (graphics-settings)
    private void ToggleGraphics()
    {
        ToggleRightPage(graphicsPage);
    }

    // EDIT (graphics-settings): Summary: Opens the given BookR page and closes the other, or closes it if it's already open.
    private void ToggleRightPage(GameObject page)
    {
        if (page == null) return;

        bool open = !page.activeSelf;

        if (debugPage != null) debugPage.SetActive(false);
        if (graphicsPage != null) graphicsPage.SetActive(false);

        page.SetActive(open);
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
