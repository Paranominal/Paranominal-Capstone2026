using UnityEngine;

public class ALTScannableObject : MonoBehaviour
{
    public GrimoireEntry entry; // i don't think this should be a scriptable object tbh i think they should just be structs but what do i know im just the programmer
    public Outline outline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        outline = GetComponent<Outline>();
        outline.enabled = false;
    }

    
}
