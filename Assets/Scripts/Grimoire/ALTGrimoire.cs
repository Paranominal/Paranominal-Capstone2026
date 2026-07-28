using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// EDIT (grimoire migration): no longer owns data. Reads from DiscoveryLog (what has been seen)
// and Inventory (what is carried). Subscribes to change events to rebuild the display.
// ALTGrimoireEntry is retired; display data comes from ItemDefinition + DiscoveryLog.DiscoveryEntry.
public class ALTGrimoire : MonoBehaviour
{
    public static ALTGrimoire instance;

    [HideInInspector] public bool grimoireActive;

    public event System.Action<bool> OnGrimoireToggled;

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

    [Header("External Systems")]
    [SerializeField] private PlayerInputReader playerInputReader;
    public PlayerHUD screenUI;

    // Internal state
    private int currentEntry;
    private List<DiscoveryLog.DiscoveryEntry> displayEntries = new List<DiscoveryLog.DiscoveryEntry>();
    private List<GameObject> entryButtons = new List<GameObject>();
    private Vector2 polaroidBasePosition;

    // System references
    private DiscoveryLog discoveryLog;
    private Inventory inventory;

    // Input Actions
    private InputAction scrollGrimoireAction;
    private InputAction grimoireUIAction;

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

        if (screenUI == null)
            screenUI = FindAnyObjectByType<PlayerHUD>();
        if (playerInputReader == null)
            playerInputReader = FindAnyObjectByType<PlayerInputReader>();
        if (discoveryLog == null)
            discoveryLog = FindAnyObjectByType<DiscoveryLog>();
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
    }

    void Start()
    {
        scrollGrimoireAction = InputSystem.actions.FindAction("ScrollGrimoire");
        grimoireUIAction = InputSystem.actions.FindAction("GrimoireUI");

        InputSystem.actions.FindActionMap("UI").Disable();

        polaroidBasePosition = imageFrameParent.rectTransform.anchoredPosition;

        // Subscribe to data changes so the display rebuilds automatically.
        if (discoveryLog != null)
            discoveryLog.OnDiscoveryChanged += RebuildEntryList;
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshCurrentEntry;

        // EDIT (grimoire-decoupling): subscribe to scan hover event.
        ScanController.OnScannableHovered += SelectByItem;

        RebuildEntryList();

        if (displayEntries.Count == 0)
        {
            imageFrameParent.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (discoveryLog != null)
            discoveryLog.OnDiscoveryChanged -= RebuildEntryList;
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshCurrentEntry;

        // EDIT (grimoire-decoupling): unsubscribe from scan hover event.
        ScanController.OnScannableHovered -= SelectByItem;
    }

    void Update()
    {
        Vector2 grimoireScroll = scrollGrimoireAction.ReadValue<Vector2>();
        if (!grimoireActive)
        {
            if (grimoireScroll.y < 0)
            {
                TurnPage(true);
            }
            else if (grimoireScroll.y > 0)
            {
                TurnPage(false);
            }
        }

        if (grimoireUIAction.WasPressedThisFrame())
        {
            if (!grimoireActive) // GRIMOIRE ACTIVATE!
            {
                SetGrimoireUnscaledTime(true);
                if (grimoireAnim != null)
                {
                    grimoireAnim.Play("up");
                }
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

                InputSystem.actions.FindActionMap("Player").Disable();
                InputSystem.actions.FindActionMap("UI").Enable();
            }
            else // GRIMOIRE AWAY!!
            {
                SetGrimoireUnscaledTime(false);
                if (grimoireAnim != null)
                {
                    grimoireAnim.Play("down");
                }
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

                InputSystem.actions.FindActionMap("Player").Enable();
                InputSystem.actions.FindActionMap("UI").Disable();
            }
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            if (entryButtons.Count > 0 && currentEntry < entryButtons.Count)
            {
                EventSystem.current.SetSelectedGameObject(entryButtons[currentEntry]);
            }
        }
    }

    private void SetGrimoireUnscaledTime(bool useUnscaledTime)
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            animator.updateMode = useUnscaledTime ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
        }
    }

    public void ForceCloseForPause()
    {
        if (!grimoireActive)
        {
            return;
        }

        SetGrimoireUnscaledTime(false);
        if (grimoireAnim != null)
        {
            grimoireAnim.Play("down");
        }

        grimoireActive = false;
        OnGrimoireToggled?.Invoke(false);

        if (screenUI != null)
        {
            screenUI.UIVisible(true);
        }

        InputSystem.actions.FindActionMap("UI")?.Disable();
    }

    // Summary: Rebuilds the entire entry button list from DiscoveryLog.
    private void RebuildEntryList()
    {
        // Clear old buttons.
        foreach (GameObject btn in entryButtons)
        {
            if (btn != null) Destroy(btn);
        }
        entryButtons.Clear();

        // Rebuild from discovery data.
        if (discoveryLog != null)
            displayEntries = discoveryLog.GetAllEntries();
        else
            displayEntries.Clear();

        for (int i = 0; i < displayEntries.Count; i++)
        {
            int entryIndex = i;
            DiscoveryLog.DiscoveryEntry entry = displayEntries[i];

            GameObject newEntryButton = Instantiate(entryButtonPrefab, listContentParent.transform);
            newEntryButton.GetComponentInChildren<TMP_Text>().text = entry.item.displayName;
            newEntryButton.GetComponent<Button>().onClick.AddListener(() => SelectEntry(entryIndex));
            entryButtons.Add(newEntryButton);
        }

        if (displayEntries.Count == 0)
        {
            imageFrameParent.gameObject.SetActive(false);
        }
        else
        {
            currentEntry = Mathf.Clamp(currentEntry, 0, displayEntries.Count - 1);
            SelectEntry(currentEntry);
        }
    }

    // Summary: Refreshes the current entry display (e.g. when inventory changes and "collected" status updates).
    private void RefreshCurrentEntry()
    {
        if (displayEntries.Count > 0 && currentEntry < displayEntries.Count)
            UpdateText();
    }

    // Summary: Called by ScanController to auto-select the grimoire page for a given item.
    public void SelectByItem(ItemDefinition item)
    {
        if (item == null) return;

        for (int i = 0; i < displayEntries.Count; i++)
        {
            if (displayEntries[i].item == item)
            {
                SelectEntry(i);
                return;
            }
        }
    }

    public void TurnPage(bool forwards)
    {
        if (displayEntries.Count != 0)
        {
            if (forwards)
            {
                if (currentEntry != displayEntries.Count - 1)
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

    // Summary: Updates the grimoire display from DiscoveryLog + Inventory data.
    public void UpdateText()
    {
        if (displayEntries.Count == 0 || currentEntry >= displayEntries.Count) return;

        DiscoveryLog.DiscoveryEntry entry = displayEntries[currentEntry];
        ItemDefinition item = entry.item;

        imageFrameParent.gameObject.SetActive(true);

        entryNameDisplay.SetText(item.displayName);

        // "collected" now means "currently in inventory".
        if (inventory != null && inventory.Has(item))
        {
            collectedDisplay.SetText("collected");
        }
        else
        {
            collectedDisplay.SetText("");
        }

        flavourTextDisplay.SetText(item.flavourText);
        hintCompletedTextDisplay.SetText(item.hintText);

        if (entry.snapshot != null)
            displayImage.texture = entry.snapshot;

        // random polaroid position/rotation
        Random.InitState(currentEntry);
        float offsetX = Random.Range(-8f, 8);
        float offsetY = Random.Range(-15f, 15);
        float rotation = Random.Range(-20f, 20f);
        imageFrameParent.rectTransform.anchoredPosition = polaroidBasePosition + new Vector2(offsetX, offsetY);
        imageFrameParent.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    public void SelectEntry(int index)
    {
        if (displayEntries.Count == 0) return;

        currentEntry = Mathf.Clamp(index, 0, displayEntries.Count - 1);
        if (entryButtons.Count > currentEntry)
        {
            EventSystem.current.SetSelectedGameObject(entryButtons[currentEntry]);
        }
        UpdateText();
    }
}
