using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;


public class ALTGrimoire : MonoBehaviour
{
    public static ALTGrimoire instance;
    // for reasons only knowable to God and the .NET development team, making the below list private prevents the AddEntry function IN THIS SCRIPT from working
    public List<grimoireEntry> entries;    // note to future programmers: this is the only critical savable data here. current entry is nice but less necessary. the scriptable object solution is less ideal imo
    private int currentEntry;
    public TextMeshProUGUI textDisplay;
    public TextMeshProUGUI collectedDisplay;

    // my general stance is there should probably be a higher level input manager than just handling this script to script but im writing this drugged out of my mind so i will NOT be doing that now
    InputAction nextPageAction;
    InputAction lastPageAction;

    public struct grimoireEntry
    { 
        public string entryName;
        public string flavourText;
        public string hintText;
        public string completeText;
        public bool collected;
    }


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
        nextPageAction = InputSystem.actions.FindAction("Next");
        lastPageAction = InputSystem.actions.FindAction("Previous");
        //scrolling through pages might work better as a scroll wheel function if we go mouse + keyboard first, but idk how you'd translate that to controller so you might need both at once
    }

    // Update is called once per frame
    void Update()
    {
        if (nextPageAction.WasPressedThisFrame())
        {
            TurnPage(true);
        }
        if (lastPageAction.WasPressedThisFrame())
        {
            TurnPage(false);
        }
    }

    public grimoireEntry ToEntry(ALTGrimoireEntry entry, bool collected)
    {
        grimoireEntry e = new grimoireEntry();
        e.entryName = entry.entryName;
        e.flavourText = entry.flavourText;
        e.hintText = entry.hintText;
        e.completeText = entry.completeText;
        e.collected = collected;
        Debug.Log(e);
        return e;
    }

    public grimoireEntry ToEntry(ALTGrimoireEntry entry)    // you already know im overloading for a default value
    {
        return ToEntry(entry, false);
    }

    public grimoireEntry GetEntry(int n)    //this could be overloaded to handle several means of accessing (by name, an ID, etc)
    {
        return entries[n];
    }

    public grimoireEntry GetEntry(string name)
    {
        foreach (grimoireEntry e in entries)
        {
            if (name == e.entryName)
            {
                return e;
            }
        }
        Debug.LogWarning("No entry of that name could be found, returning empty entry.");
        grimoireEntry g = new grimoireEntry();
        return g;
   
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

    public grimoireEntry GetCurrentEntry()
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
                UpdateText();
            }
        }
        else
        {
            if (currentEntry != 0)
            {
                currentEntry--;
                UpdateText();
            }
        }
    }

    public void UpdateText()    //handling this here for now while the rest of the grimoire gets written, should probably NOT ship with this functionality
    {
        textDisplay.SetText(GetCurrentEntry().entryName);
        if (GetCurrentEntry().collected)
        {
            collectedDisplay.SetText("collected");
        }
        else
        {
            collectedDisplay.SetText("");
        }
    }

    public void AddEntry(grimoireEntry entry, bool collected)
    {
        //Debug.Log(entry);
        if (!CompareEntry(entry))   // checks the item hasn't already been added to the grimoire
        {
            entry.collected = collected;
            entries.Add(entry);
            currentEntry = entries.Count - 1;   //automatically switches to new entry
            UpdateText();
        }

    }

    public void AddEntry(grimoireEntry entry)   //overload assumes that you're not specifying bc it hasnt been collected/isnt collectable
    {
        AddEntry(entry, false);
    }

    public bool CompareEntry(grimoireEntry entry) // Returns True if entry is already in the entry list, and False if not
    {
        if (entries.Count > 0)
        {
            foreach (grimoireEntry e in entries)
            {
                if (entry.entryName == e.entryName)   // adding an ID system would allow multiple items to have the same name field although that may be confusing
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void CollectEntry(grimoireEntry entry, bool status)  //changes entry's collection status. optional bool mostly just exists in case we need to uncollect things down the track.
    {
        grimoireEntry e = entries[GetEntryID(entry.entryName)];
        e.collected = status;
        entries[GetEntryID(entry.entryName)] = e;
    }

    public void CollectEntry(grimoireEntry entry)   // true set as default since like. thats what collecting something is.
    {
        CollectEntry(entry, true);
    }
}
