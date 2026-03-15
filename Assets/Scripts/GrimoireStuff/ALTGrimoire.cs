using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class ALTGrimoire : MonoBehaviour
{
    public static ALTGrimoire instance;
    // for reasons only knowable to God and the .NET development team, making the below list private prevents the AddEntry function IN THIS SCRIPT from working
    public List<GrimoireEntry> entries;    // note to future programmers: this is the only critical savable data here. current entry is nice but less necessary. the scriptable object solution is less ideal imo
    private int currentEntry;
    public TextMeshProUGUI textDisplay;

    // my general stance is there should probably be a higher level input manager than just handling this script to script but im writing this drugged out of my mind so i will NOT be doing that now
    InputAction nextPageAction;
    InputAction lastPageAction;


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


    public GrimoireEntry GetEntry(int n)    //this could be overloaded to handle several means of accessing (by name, an ID, etc)
    {
        return entries[n];
    }

    public GrimoireEntry GetCurrentEntry()
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
        textDisplay.SetText(GetCurrentEntry().name);
    }

    public void AddEntry(GrimoireEntry entry)
    {
        //Debug.Log(entry);
        if (!CompareEntry(entry))   // checks the item hasn't already been added to the grimoire
        {
            entries.Add(entry);
            currentEntry = entries.Count - 1;   //automatically switches to new entry
            UpdateText();
        }

    }

    public bool CompareEntry(GrimoireEntry entry) // Returns True if entry is already in the entry list, and False if not
    {
        foreach (GrimoireEntry e in entries)
        {
            if (entry.name == e.name)   // adding an ID system would allow multiple items to have the same name field although that may be confusing
            {
                return true;
            }
        }

        return false;
    }
}
