using System;
using UnityEngine;

public class CauldronColours : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private Container container;
    private int containedHerbs = 0;

    void Awake()
    {
        material.color = new Color(0.5f, 0.5f, 0.5f);
    }
    // Update is called once per frame
    void Update()
    {
        if (container.contents.Count > containedHerbs)
        {
            if (container.contents[containedHerbs].entryName == "Blue Herb") AddBlue();
            else if (container.contents[containedHerbs].entryName == "Green Herb") AddGreen();
            else if (container.contents[containedHerbs].entryName == "Purple Herb") AddPurple();
            else if (container.contents[containedHerbs].entryName == "Orange Herb") AddOrange();
            else Debug.Log("failed to change cauldron colour with: " + container.contents[containedHerbs].entryName);
        }
    }
    
    private void AddBlue()
    {
        containedHerbs++;
        material.color = new Color(0f, 0f, 1f);
    }
    private void AddGreen()
    {
        containedHerbs++;
        material.color = new Color(0f, 1f, 0f);

    }
    private void AddOrange()
    {
        containedHerbs++;
        material.color = new Color(0.6f, 0.4f, 0f);
    }
    private void AddPurple()
    {
        containedHerbs++;
        material.color = new Color(0.3f, 0f, 0.7f);
    }
}
