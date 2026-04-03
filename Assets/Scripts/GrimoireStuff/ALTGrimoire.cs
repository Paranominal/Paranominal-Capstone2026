using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ALTGrimoire : MonoBehaviour
{
    public static ALTGrimoire instance;
    // for reasons only knowable to God and the .NET development team, making the below list private prevents the AddEntry function IN THIS SCRIPT from working
    public List<ALTGrimoireEntry> entries;    // note to future programmers: this is the only critical savable data here. current entry is nice but less necessary. the scriptable object solution is less ideal imo
    private int currentEntry;
    private List<GameObject> entryButtons = new List<GameObject>();
    public TextMeshProUGUI entryNameDisplay;
    public TextMeshProUGUI collectedDisplay;
    public TextMeshProUGUI flavourTextDisplay;
    public TextMeshProUGUI hintCompletedTextDisplay;
    public GameObject listContentParent;
    public GameObject entryButtonPrefab;
    public bool grimoireActive;
    public PlayerMovement playerScript;

    public Animator grimoireAnim;

    // my general stance is there should probably be a higher level input manager than just handling this script to script but im writing this drugged out of my mind so i will NOT be doing that now
    InputAction scrollGrimoireAction;
    InputAction grimoireUIAction;


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
    }

    void Start()
    {
        scrollGrimoireAction = InputSystem.actions.FindAction("ScrollGrimoire");
        grimoireUIAction = InputSystem.actions.FindAction("GrimoireUI");

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

        if (grimoireUIAction.WasPressedThisFrame())
        {
            if (!grimoireActive)
            {
                grimoireAnim.Play("up");
                grimoireActive = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                playerScript.canMove = false;
            }
            else
            {
                grimoireAnim.Play("down");
                grimoireActive = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                playerScript.canMove = true;
            }
        }
    }

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

    public void UpdateText()    //handling this here for now while the rest of the grimoire gets written, should probably NOT ship with this functionality
    {
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

            SelectEntry(currentEntry);
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
        if (entryButtons.Count > index)
        {
            EventSystem.current.SetSelectedGameObject(entryButtons[index]);
        }
        UpdateText();
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
        return e;
    }
}
