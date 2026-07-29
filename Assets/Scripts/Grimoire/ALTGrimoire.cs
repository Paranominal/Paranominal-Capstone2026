using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Summary: Full Grimoire UI controller. Handles open/close toggle (Tab key),
// time pause, cursor state, action map switching, and main tab navigation.
// Content is delegated to per-tab panel scripts (GrimoireInventoryPanel, etc.)
// which all write into the shared ScrollView on BookL and detail view on BookR.
// EDIT (grimoire redesign): reworked from flat discovery list to tabbed panel architecture.
public class ALTGrimoire : MonoBehaviour
{
    public static ALTGrimoire instance;

    [HideInInspector] public bool grimoireActive;

    public event System.Action<bool> OnGrimoireToggled;

    [Header("Panels")]
    [Tooltip("Empty GameObjects holding the panel scripts. Not visual elements.")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject spellsPanel;
    [SerializeField] private GameObject weaponsPanel;
    [SerializeField] private GameObject recipesPanel;
    [SerializeField] private GameObject bestiaryPanel;

    [Header("Main Tab Buttons (left edge of BookL)")]
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button spellsTabButton;
    [SerializeField] private Button weaponsTabButton;
    [SerializeField] private Button recipesTabButton;
    [SerializeField] private Button bestiaryTabButton;

    [Header("Tab Visuals")]
    [SerializeField] private Color tabNormalColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color tabActiveColor = new Color(1f, 1f, 1f, 1f);

    [Header("Shared UI")]
    [Tooltip("Heading text on BookL. Updated to show the active tab name.")]
    [SerializeField] private TMP_Text headingText;

    [Header("Content Containers")]
    [Tooltip("Parent of all Full Grimoire UI elements on BookL.")]
    [SerializeField] private GameObject fullContentL;
    [Tooltip("Parent of all Full Grimoire UI elements on BookR.")]
    [SerializeField] private GameObject fullContentR;
    [Tooltip("Parent of minimised UI elements on BookL (spirit meter).")]
    [SerializeField] private GameObject minimisedContentL;
    [Tooltip("Parent of minimised UI elements on BookR (quick-slots).")]
    [SerializeField] private GameObject minimisedContentR;

    [Header("Animation")]
    [SerializeField] private Animator grimoireAnim;

    [Header("External Systems")]
    [SerializeField] private PlayerInputReader playerInputReader;
    [SerializeField] private PlayerHUD screenUI;

    private GrimoireTab activeTab = GrimoireTab.Inventory;
    private InputAction grimoireUIAction;

    private GameObject[] panels;
    private Button[] tabButtons;

    private static readonly string[] tabNames = { "Inventory", "Spells", "Weapons", "Recipes", "Bestiary" };

    private void Awake()
    {
        if (instance != null)
            Debug.LogWarning("Found more than one Grimoire in the scene");
        else
            instance = this;

        if (screenUI == null)
            screenUI = FindAnyObjectByType<PlayerHUD>();
        if (playerInputReader == null)
            playerInputReader = FindAnyObjectByType<PlayerInputReader>();
        if (grimoireAnim == null)
            grimoireAnim = GetComponentInChildren<Animator>();

        panels = new GameObject[] { inventoryPanel, spellsPanel, weaponsPanel, recipesPanel, bestiaryPanel };
        tabButtons = new Button[] { inventoryTabButton, spellsTabButton, weaponsTabButton, recipesTabButton, bestiaryTabButton };
    }

    private void Start()
    {
        grimoireUIAction = InputSystem.actions.FindAction("GrimoireUI");
        InputSystem.actions.FindActionMap("UI").Disable();

        SetupTabButtons();

        // Start with all panels disabled.
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].SetActive(false);
        }

        // Start in minimised mode (full grimoire is closed).
        SetContentMode(full: false);
    }

    private void Update()
    {
        if (grimoireUIAction != null && grimoireUIAction.WasPressedThisFrame())
        {
            if (!grimoireActive)
                OpenGrimoire();
            else
                CloseGrimoire();
        }
    }

    // ---- Open / Close ----

    private void OpenGrimoire()
    {
        SetGrimoireUnscaledTime(true);

        if (grimoireAnim != null)
            grimoireAnim.Play("up");

        grimoireActive = true;
        OnGrimoireToggled?.Invoke(true);
        Time.timeScale = 0f;

        if (playerInputReader != null)
            playerInputReader.SetCursorState(CursorLockMode.None, true);
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (screenUI != null)
            screenUI.UIVisible(false);

        // Show full grimoire content, hide minimised content.
        SetContentMode(full: true);

        InputSystem.actions.FindActionMap("Player").Disable();
        InputSystem.actions.FindActionMap("UI").Enable();

        SwitchTab(activeTab);
    }

    private void CloseGrimoire()
    {
        // Disable active panel before closing so it clears the list.
        int activeIndex = (int)activeTab;
        if (activeIndex < panels.Length && panels[activeIndex] != null)
            panels[activeIndex].SetActive(false);

        SetGrimoireUnscaledTime(false);

        if (grimoireAnim != null)
            grimoireAnim.Play("down");

        grimoireActive = false;
        OnGrimoireToggled?.Invoke(false);
        Time.timeScale = 1f;

        if (playerInputReader != null)
            playerInputReader.SetCursorState(CursorLockMode.Locked, false);
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (screenUI != null)
            screenUI.UIVisible(true);

        // Show minimised content, hide full grimoire content.
        SetContentMode(full: false);

        InputSystem.actions.FindActionMap("Player").Enable();
        InputSystem.actions.FindActionMap("UI").Disable();
    }

    // Summary: Force-close when the pause menu opens.
    public void ForceCloseForPause()
    {
        if (!grimoireActive) return;

        int activeIndex = (int)activeTab;
        if (activeIndex < panels.Length && panels[activeIndex] != null)
            panels[activeIndex].SetActive(false);

        SetGrimoireUnscaledTime(false);

        if (grimoireAnim != null)
            grimoireAnim.Play("down");

        grimoireActive = false;
        OnGrimoireToggled?.Invoke(false);

        if (screenUI != null)
            screenUI.UIVisible(true);

        SetContentMode(full: false);

        InputSystem.actions.FindActionMap("UI")?.Disable();
    }

    // ---- Tab Switching ----

    private void SetupTabButtons()
    {
        if (inventoryTabButton != null)
            inventoryTabButton.onClick.AddListener(() => SwitchTab(GrimoireTab.Inventory));
        if (spellsTabButton != null)
            spellsTabButton.onClick.AddListener(() => SwitchTab(GrimoireTab.Spells));
        if (weaponsTabButton != null)
            weaponsTabButton.onClick.AddListener(() => SwitchTab(GrimoireTab.Weapons));
        if (recipesTabButton != null)
            recipesTabButton.onClick.AddListener(() => SwitchTab(GrimoireTab.Recipes));
        if (bestiaryTabButton != null)
            bestiaryTabButton.onClick.AddListener(() => SwitchTab(GrimoireTab.Bestiary));
    }

    // Summary: Show the selected tab's panel and hide the others.
    // All panels are disabled first to ensure the old panel's OnDisable (which clears
    // the shared list) runs before the new panel's OnEnable (which rebuilds it).
    public void SwitchTab(GrimoireTab tab)
    {
        activeTab = tab;
        int activeIndex = (int)tab;

        // Disable all panels first.
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].SetActive(false);
        }

        // Then enable the target panel.
        if (activeIndex < panels.Length && panels[activeIndex] != null)
            panels[activeIndex].SetActive(true);

        if (headingText != null && activeIndex < tabNames.Length)
            headingText.SetText(tabNames[activeIndex]);

        UpdateTabButtonVisuals(activeIndex);
    }

    private void UpdateTabButtonVisuals(int activeIndex)
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;

            Image btnImage = tabButtons[i].GetComponent<Image>();
            if (btnImage != null)
                btnImage.color = (i == activeIndex) ? tabActiveColor : tabNormalColor;
        }
    }

    // ---- Utilities ----

    // Summary: Toggle between full grimoire content and minimised content.
    // Full = tabs, lists, detail view. Minimised = quick-slots, spirit meter.
    private void SetContentMode(bool full)
    {
        if (fullContentL != null) fullContentL.SetActive(full);
        if (fullContentR != null) fullContentR.SetActive(full);
        if (minimisedContentL != null) minimisedContentL.SetActive(!full);
        if (minimisedContentR != null) minimisedContentR.SetActive(!full);
    }

    private void SetGrimoireUnscaledTime(bool useUnscaledTime)
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
            animator.updateMode = useUnscaledTime
                ? AnimatorUpdateMode.UnscaledTime
                : AnimatorUpdateMode.Normal;
    }
}
