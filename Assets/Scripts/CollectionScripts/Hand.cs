using UnityEngine;

public class Hand : MonoBehaviour
{
    public static Hand instance;
    public ALTScannableObject holding = null;   //the item class here might change, and probably to a custom one for it with relevant data points (prefab for setting back down, location possibly?)
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Collect(ALTScannableObject item)
    {
        if (holding == null)
        {
            holding = item;
            Debug.Log("Oh wow you picked up " + holding);
            //this is where you need to deactivate the shotgun (ideally a single script call) and update the visual
            //should also quite possibly destroy the item on pick up but for now we're not doing that so
        }
        else
        {
            Debug.LogWarning("You're already holding an item silly bugger, it's " + holding);   //I don't know what the intended behaviour on picking something up with something in your hand is
        }
    }

    public void Drop()
    {
        Debug.Log("You were holding " + holding + " but then you dropped it. I don't know why.");
        holding = null;
        //this should include further behaviour to put the item back. how is that handled though?
    }
}
