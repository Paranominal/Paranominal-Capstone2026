using UnityEngine;
using System.Collections.Generic;
public class Container : MonoBehaviour, IInteractable
{
    private ALTGrimoire grimoire;
    public List<ALTGrimoireEntry> contents;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Interact()
    {
        contents.Add(grimoire.GetCurrentEntry());
    }

    public void Empty()
    {
        contents.Clear();
    }
}
