using System.Collections.Generic;
using System.ComponentModel.Design;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WeakPoint : MonoBehaviour
{
    public WeakPointManager manager;
    public WeakPointType weakPointType;
    [SerializeField] private GameObject ironElement;
    [SerializeField] private GameObject silverElement;
    private GameObject currentElement;
    void Awake()
    {
        if (weakPointType == WeakPointType.Iron) currentElement = ironElement;
        else if (weakPointType == WeakPointType.Silver) currentElement = silverElement;
        else Debug.Log(gameObject + " is broken!! : weakpoint type is somehow neither iron nor silver!");
    }
    public void Show(WeakPointType weakPointType)
    {
        gameObject.GetComponent<SphereCollider>().enabled = true; //activates collider   
        SpriteRenderer[] renderers = currentElement.GetComponentsInChildren<SpriteRenderer>(); //gets renderers in the correct element
        foreach (SpriteRenderer renderer in renderers) renderer.enabled = true; //activates the gotten renderers
    }
    public void Hide()
    {
        gameObject.GetComponent<SphereCollider>().enabled = false; //deactivates collider   
        SpriteRenderer[] renderers = gameObject.GetComponentsInChildren<SpriteRenderer>(); //gets ALL renderers in children
        foreach (SpriteRenderer renderer in renderers) renderer.enabled = false; //deactivates all the gotten renderers
    }
    
    public void OnHit(WeakPointType type)
    {
        if (type != weakPointType) return; //don't continue if bullet type is incorrect

        Hide();
        manager.NextWeakPoint();
    }
}
