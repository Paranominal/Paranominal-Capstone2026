using UnityEngine;
public enum ScanCategory { Object, Enemy, KeyItem }

public class ALTScannableObject : MonoBehaviour
{
    public ALTGrimoireEntry entry; 
    public Outline outline;
    public bool collectable;
    public ScanCategory category = ScanCategory.Object;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        outline = GetComponent<Outline>();
        outline.enabled = false;
        outline.OutlineWidth = 5f;
        outline.OutlineMode = Outline.Mode.OutlineVisible;
        ScanModeVisuals.instance.RegisterScannable(this);
    }
}
