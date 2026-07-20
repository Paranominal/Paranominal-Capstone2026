using System;
using UnityEditor;
using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    [SerializeField] private GameObject textDisplay;
    [SerializeField] private Outline outline;
    // [HideInInspector] public bool isHovered;
    void Start()
    {
        Deactivate();
    }
    public void Activate()
    {
        if (textDisplay != null) textDisplay.SetActive(true);
        if (outline != null) outline.enabled = true;
        // isHovered = true;
    }
    public void Deactivate()
    {
        if (textDisplay != null) textDisplay.SetActive(false);
        if (outline != null) outline.enabled = false;
        // isHovered = false;
    }
}
