using UnityEngine;

// Summary: Common interface for anything the player can scan (world items, enemies, lore objects).
// ScanController talks exclusively to this interface for scan logic and outline control.
public interface IScannable
{
    float ScanDuration { get; }

    // Summary: True if this object has already been registered in its target log (DiscoveryLog or Bestiary).
    bool IsDiscovered { get; }

    // Summary: True if scanning should still progress even after discovery (e.g. enemies for stagger).
    bool IsRescannable { get; }

    // Summary: Called by ScanController when the scan bar completes. Snapshot may be null.
    void OnScanComplete(Texture2D snapshot);

    void SetOutlineVisible(bool visible);
    void SetOutlineColor(Color color);
}
