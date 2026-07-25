using System;
using UnityEditor;
using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    [SerializeField] private GameObject textDisplay;
    public ALTGrimoireEntry grimoireEntry;
    [SerializeField] private GameObject pickupDialogue;
    [HideInInspector] public Dialogue dialogue;
    [SerializeField] private Outline outline;
    void Start()
    {
        dialogue = pickupDialogue.GetComponent<Dialogue>();
        //Debug.Log($"{dialogue.itemName} | {dialogue} | {dialogue.gameObject}");
        Deactivate();
    }
    public void Activate()
    {
        if (textDisplay != null) textDisplay.SetActive(true);
        if (outline != null) outline.enabled = true;
    }
    public void Deactivate()
    {
        if (textDisplay != null) textDisplay.SetActive(false);
        if (outline != null) outline.enabled = false;
    }
}
