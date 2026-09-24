using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Summary: Grimoire controller. Holds the entries list (the player's items) and switches the book between two content modes:
// the Minimised UI (current entry display + scrollable entry list) during gameplay, and the Full Interface, which is the pause menu.
// Pausing goes through PauseManager. Full Interface tab content is delegated to the panel scripts (GrimoireInventoryPanel, etc.).
// EDIT (grimoire-pause): Full Interface ported from the mid-year prototype. Pause, cursor and action map handling moved to PauseManager.
public class ALTGrimoire : MonoBehaviour
{
    public static ALTGrimoire instance;

    [Header("Data Management")]
    [Tooltip("Master list of all entries currently in the Grimoire")]
    // for reasons only knowable to God and the .NET development team, making the below list private prevents the AddEntry function IN THIS SCRIPT from working
    public List<ALTGrimoireEntry> entries;    // note to future programmers: this is the only critical savable data here. current entry is nice but less necessary. the scriptable object solution is less ideal imo
    
    [HideInInspector] public bool grimoireActive;

    public event System.Action<bool> OnGrimoireToggled;

    // EDIT (grimoire-pause): raised when an entry is added or its collected state changes. Used by GrimoireInventoryPanel.
    public event System.Action OnEntriesChanged;

    // EDIT (grimoire-pause): exposed so the Inventory page can highlight the current item.
    public int CurrentEntryIndex => currentEntry;

    [Header("UI References: Content")]
    [SerializeField] private TextMeshProUGUI entryNameDisplay;
    [SerializeField] private TextMeshProUGUI collectedDisplay;
    [SerializeField] private TextMeshProUGUI flavourTextDisplay;
    [SerializeField] private TextMeshProUGUI hintCompletedTextDisplay;
    [SerializeField] private RawImage displayImage;
    [SerializeField] private RawImage imageFrameParent;

    [Header("UI References: Navigation")]
    [SerializeField] private GameObject listContentParent;
    [SerializeField] private GameObject entryButtonPrefab;
    [SerializeField] private Animator grimoireAnim;

    // EDIT (grimoire-pause): Full Interface references.
    [Header("Full Interface: Toggle")]
    [Tooltip("Any of these actions opens and closes the Full Interface (e.g. GrimoireUI on Tab, Pause on Escape). Falls back to GrimoireUI if empty.")]
    [SerializeField] private InputActionReference[] toggleActions;
    [Tooltip("The X button that closes the Full Interface.")]
    [SerializeField] private Button closeButton;

    [Header("Full Interface: Panels")]
    [Tooltip("Empty GameObjects holding the panel scripts. Not visual elements.")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject bestiaryPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Full Interface: Tab Buttons")]
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button bestiaryTabButton;
    [SerializeField] private Button settingsTabButton;
    [SerializeField] private Color tabNormalColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color tabActiveColor = new Color(1f, 1f, 1f, 1f);

    [Header("Full Interface: Layout")]
    [Tooltip("Heading text on BookL. Shows the active tab name.")]
    [SerializeField] private TMP_Text headingText;
    [Tooltip("The shared ScrollView (BookL) and DetailView (BookR) used by the Inventory and Bestiary tabs. Hidden on the Settings tab.")]
    [SerializeField] private GameObject[] sharedListLayout;

    [Header("Content Containers")]
    [Tooltip("Parent of all Full Interface elements on BookL.")]
    [SerializeField] private GameObject fullContentL;
    [Tooltip("Parent of all Full Interface elements on BookR.")]
    [SerializeField] private GameObject fullContentR;
    [Tooltip("Parent of the Minimised UI elements on BookL.")]
    [SerializeField] private GameObject minimisedContentL;
    [Tooltip("Parent of the Minimised UI elements on BookR.")]
    [SerializeField] private GameObject minimisedContentR;

    [Header("External Systems")]
    public PhotoSnapshots snapshotHandler;
    public PlayerHUD screenUI;
    // EDIT (grimoire-pause): replaces the old PlayerInputReader reference. Cursor state is handled by PauseManager now.
    [SerializeField] private PauseManager pauseManager;

    // Internal Navigation State
    private int currentEntry;
    private List<GameObject> entryButtons = new List<GameObject>();
    private Vector2 polaroidBasePosition;

    // EDIT (grimoire-pause): Full Interface state.
    private GrimoireTab activeTab = GrimoireTab.Inventory;
    private GameObject[] panels;
    private Button[] tabButtons;
    private static readonly string[] tabNames = { "Inventory", "Bestiary", "Settings" };

    // Input Actions
    private InputAction scrollGrimoireAction;
    // EDIT (grimoire-pause): replaces grimoireUIAction.
    private List<InputAction> toggles = new List<InputAction>();


    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one Grimoire in the scene");
        }
        else
        {
            instance = this;
        }

        // EDIT (grimoire-pause): arrays in GrimoireTab order.
        panels = new GameObject[] { inventoryPanel, bestiaryPanel, settingsPanel };
        tabButtons = new Button[] { inventoryTabButton, bestiaryTabButton, settingsTabButton };
    }

    void Start()
    {
        scrollGrimoireAction = InputSystem.actions.FindAction("ScrollGrimoire");

        // EDIT (grimoire-pause): UI map disable moved to PauseManager. Cross-prefab fallbacks.
        if (pauseManager == null)
            pauseManager = PauseManager.Instance != null ? PauseManager.Instance : FindAnyObjectByType<PauseManager>();
        if (screenUI == null)
            screenUI = FindAnyObjectByType<PlayerHUD>();

        // EDIT (grimoire-pause): Full Interface setup. Starts closed with the Minimised UI showing.
        SetupToggleActions();
        SetupButtons();
        DisableAllPanels();
        SetContentMode(full: false);

        polaroidBasePosition = imageFrameParent.rectTransform.anchoredPosition;

        if (entries != null) // sanity check
        {
            for (int i = 0; i < entries.Count; i++)
            {
                int currentIndex = i;
                GameObject newEntryButton = Instantiate(entryButtonPrefab, listContentParent.transform);
                newEntryButton.GetComponentInChildren<TMP_Text>().text = entries[i].entryName;
                newEntryButton.GetComponent<Button>().onClick.AddListener(() => SelectEntry(currentIndex));
                entryButtons.Add(newEntryButton);
            }
        }

        if (entries.Count == 0) 
        { 
            imageFrameParent.gameObject.SetActive(false);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 grimoireScroll = scrollGrimoireAction.ReadValue<Vector2>();
        if (!grimoireActive)
        {
            if(grimoireScroll.y < 0)
            {
                TurnPage(true);
            }
            else if(grimoireScroll.y > 0)
            {
                TurnPage(false);
            }
        }

        // EDIT (grimoire-pause): open/close logic moved into OpenGrimoire and CloseGrimoire.
        if (TogglePressedThisFrame())
        {
            if (!grimoireActive) OpenGrimoire(); // GRIMOIRE ACTIVATE!
            else CloseGrimoire(); // GRIMOIRE AWAY!!
        }

        // EDIT (grimoire-pause): only while closed. The Full Interface has its own buttons and would lose focus to the hidden list.
        if (!grimoireActive && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null) 
        {
            SelectCurrentEntryButton(); // grabbing the current entry selection if it drops off
        }
    }

    // ---- Open / Close ----

    // EDIT (grimoire-pause): Summary: Opens the Full Interface and pauses the game through PauseManager.
    // Ignored if something else (e.g. dialogue) currently owns the pause, or released it this frame.
    private void OpenGrimoire()
    {
        if (pauseManager != null && (pauseManager.IsPaused || pauseManager.ResumedThisFrame))
            return;

        grimoireActive = true;
        SetGrimoireUnscaledTime(true);

        if (pauseManager != null)
            pauseManager.PauseGame();

        if (screenUI != null)
            screenUI.UIVisible(false);

        SetContentMode(full: true);
        SwitchTab(activeTab);

        OnGrimoireToggled?.Invoke(true);
    }

    // EDIT (grimoire-pause): Summary: Closes the Full Interface and resumes the game. Public for the X button and Quit to Menu.
    public void CloseGrimoire()
    {
        if (!grimoireActive) return;

        // Disable panels first so they clear the shared list.
        DisableAllPanels();
        SetGrimoireUnscaledTime(false);

        grimoireActive = false;
        SetContentMode(full: false);

        if (screenUI != null)
            screenUI.UIVisible(true);

        if (pauseManager != null)
            pauseManager.ResumeGame();

        SelectCurrentEntryButton();

        OnGrimoireToggled?.Invoke(false);
    }

    private void SetGrimoireUnscaledTime(bool useUnscaledTime)
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            animator.updateMode = useUnscaledTime ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
        }
    }

    // EDIT (grimoire-pause): ForceCloseForPause removed. The Grimoire is the pause menu now, so nothing else needs to force it closed.

    // ---- Full Interface ----

    // EDIT (grimoire-pause): Summary: Collects the toggle actions and registers them with PauseManager so they keep working while paused.
    private void SetupToggleActions()
    {
        if (toggleActions != null)
        {
            foreach (InputActionReference reference in toggleActions)
            {
                if (reference != null && reference.action != null && !toggles.Contains(reference.action))
                    toggles.Add(reference.action);
            }
        }

        // Fallback to the original GrimoireUI action if nothing is assigned.
        if (toggles.Count == 0)
        {
            InputAction fallback = InputSystem.actions.FindAction("GrimoireUI");
            if (fallback != null) toggles.Add(fallback);
        }

        if (pauseManager != null)
        {
            foreach (InputAction action in toggles)
                pauseManager.RegisterPersistentAction(action);
        }
    }

    // EDIT (grimoire-pause): single toggle per frame, even if more than one toggle key is pressed.
    private bool TogglePressedThisFrame()
    {
        foreach (InputAction action in toggles)
        {
            if (action.WasPressedThisFrame()) return true;
        }
        return false;
    }

    // EDIT (grimoire-pause): tab buttons and the X button.
    private void SetupButtons()
    {
        if (inventoryTabButton != null)
            inventoryTabButton.onClick.AddListener(() => SwitchTab(GrimoireTab.Inventory));
        if (bestiaryTabButton != null)
            bestiaryTabButton.onClick.AddListener(() => SwitchTab(GrimoireTab.Bestiary));
        if (settingsTabButton != null)
            settingsTabButton.onClick.AddListener(() => SwitchTab(GrimoireTab.Settings));
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseGrimoire);
    }

    // EDIT (grimoire-pause): Summary: Shows the selected tab's panel and hides the others.
    // All panels are disabled first so the old panel's OnDisable (clears the shared list) runs before the new panel's OnEnable (rebuilds it).
    public void SwitchTab(GrimoireTab tab)
    {
        activeTab = tab;
        int activeIndex = (int)tab;

        DisableAllPanels();

        // Settings uses its own pages instead of the shared list and detail view.
        bool showSharedLayout = tab != GrimoireTab.Settings;
        if (sharedListLayout != null)
        {
            foreach (GameObject layoutObject in sharedListLayout)
            {
                if (layoutObject != null) layoutObject.SetActive(showSharedLayout);
            }
        }

        if (activeIndex < panels.Length && panels[activeIndex] != null)
            panels[activeIndex].SetActive(true);

        if (headingText != null && activeIndex < tabNames.Length)
            headingText.SetText(tabNames[activeIndex]);

        UpdateTabButtonVisuals(activeIndex);
    }

    private void DisableAllPanels()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].SetActive(false);
        }
    }

    private void UpdateTabButtonVisuals(int activeIndex)
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;

            Image buttonImage = tabButtons[i].GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = (i == activeIndex) ? tabActiveColor : tabNormalColor;
        }
    }

    // EDIT (grimoire-pause): Summary: Swaps between the Full Interface (open) and the Minimised UI (closed).
    private void SetContentMode(bool full)
    {
        if (fullContentL != null) fullContentL.SetActive(full);
        if (fullContentR != null) fullContentR.SetActive(full);
        if (minimisedContentL != null) minimisedContentL.SetActive(!full);
        if (minimisedContentR != null) minimisedContentR.SetActive(!full);
    }

    // ---- Entries ----

    public ALTGrimoireEntry GetEntry(int n)    //this could be overloaded to handle several means of accessing (by name, an ID, etc)
    {
        return entries[n];
    }

    public ALTGrimoireEntry GetEntry(string name)
    {
        foreach (ALTGrimoireEntry e in entries)
        {
            if (name == e.entryName)
            {
                return e;
            }
        }
        Debug.LogWarning("No entry of that name could be found, returning null.");
        return null;
   
    }

    public int GetEntryID(string name)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (name == entries[i].entryName)
            {
                return i;
            }
        }
        Debug.LogWarning("No entry of that name could be found, returning 0.");
        return 0;
    }

    public ALTGrimoireEntry GetCurrentEntry()
    {
        return GetEntry(currentEntry);
    }

    public void TurnPage(bool forwards) //this is maybe not an ideal function name since OTHER things might be in the grimoire but it works for now
    {
        if (entries.Count != 0)
        {
            if (forwards)
            {
                if (currentEntry != entries.Count - 1)
                {
                    currentEntry++;
                    SelectEntry(currentEntry);
                }
            }
            else
            {
                if (currentEntry != 0)
                {
                    currentEntry--;
                    SelectEntry(currentEntry);
                }
            }
        }
    }

    public void UpdateText()    //handling this here for now while the rest of the grimoire gets written, should probably NOT ship with this functionality
    {
        imageFrameParent.gameObject.SetActive(true);

        entryNameDisplay.SetText(GetCurrentEntry().entryName);
        if (GetCurrentEntry().collected)
        {
            collectedDisplay.SetText("collected");
        }
        else
        {
            collectedDisplay.SetText("");
        }
        flavourTextDisplay.SetText(GetCurrentEntry().flavourText);
        hintCompletedTextDisplay.SetText(GetCurrentEntry().hintText);
        displayImage.texture = GetCurrentEntry().snapshotImage;

        // random polaroid position/rotation
        Random.InitState(currentEntry);
        float offsetX = Random.Range(-8f, 8); // increasing the x amount by too much increases the likelihood of overlapping text
        float offsetY = Random.Range(-15f, 15); 
        float rotation = Random.Range(-20f, 20f);
        imageFrameParent.rectTransform.anchoredPosition = polaroidBasePosition + new Vector2(offsetX, offsetY);
        imageFrameParent.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    public void AddEntry(ALTGrimoireEntry entry, bool collected)
    {
        ALTGrimoireEntry e = Clone(entry);
        if (!CompareEntry(e))   // checks the item hasn't already been added to the grimoire
        {
            e.collected = collected;
            entries.Add(e);
            currentEntry = entries.Count - 1;   // switches to new entry about to be displayed
            int entryIndex = currentEntry;

            GameObject newEntryButton = Instantiate(entryButtonPrefab, listContentParent.transform);
            newEntryButton.GetComponentInChildren<TMP_Text>().text = e.entryName;
            newEntryButton.GetComponent<Button>().onClick.AddListener(() => SelectEntry(entryIndex)); // i don't know what a lambda expression does and at this point im too afraid to ask
            entryButtons.Add(newEntryButton);

            e.snapshotImage = snapshotHandler.TakeSnapshot();

            SelectEntry(currentEntry);

            // EDIT (grimoire-pause): notify the Inventory page.
            OnEntriesChanged?.Invoke();
        }
        else
        {
            SelectEntry(GetEntryID(entry.entryName)); // opens the relevant entry when scanning something already logged
        }
    }

    public void AddEntry(ALTGrimoireEntry entry)   //overload assumes that you're not specifying bc it hasnt been collected/isnt collectable
    {
        AddEntry(entry, false);
    }

    public void SelectEntry(int index)
    {
        currentEntry = index;
        // EDIT (grimoire-pause): skip EventSystem selection while the Full Interface is open, it would pull focus onto the hidden list.
        if (!grimoireActive)
        {
            SelectCurrentEntryButton();
        }
        UpdateText();
    }

    // EDIT (grimoire-pause): pulled out of SelectEntry and Update so CloseGrimoire can reuse it.
    private void SelectCurrentEntryButton()
    {
        if (EventSystem.current == null) return;

        if (entryButtons.Count > 0 && currentEntry < entryButtons.Count)
        {
            EventSystem.current.SetSelectedGameObject(entryButtons[currentEntry]);
        }
    }

    public bool CompareEntry(ALTGrimoireEntry entry) // Returns True if entry is already in the entry list, and False if not
    {

        if (entries != null)
        {
            foreach (ALTGrimoireEntry e in entries)
            {
                if (entry.entryName == e.entryName)   // adding an ID system would allow multiple items to have the same name field although that may be confusing
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void CollectEntry(ALTGrimoireEntry entry, bool status)  //changes entry's collection status. optional bool mostly just exists in case we need to uncollect things down the track.
    {
        entries[GetEntryID(entry.entryName)].collected = status;
        UpdateText();

        // EDIT (grimoire-pause): notify the Inventory page.
        OnEntriesChanged?.Invoke();
    }

    public void CollectEntry(ALTGrimoireEntry entry)   // true set as default since like. thats what collecting something is.
    {
        CollectEntry(entry, true);
    }

    public ALTGrimoireEntry Clone(ALTGrimoireEntry entry)
    {
        ALTGrimoireEntry e = new ALTGrimoireEntry();
        e.entryName = entry.entryName;
        e.flavourText = entry.flavourText;
        e.hintText = entry.hintText;
        e.completeText = entry.completeText;
        e.collected = entry.collected;
        e.snapshotImage = entry.snapshotImage;
        return e;
    }
}
