using UnityEngine;

// Summary: A world-placed item that can be scanned, collected, or both.
// Implements IScannable for the scan system and IInteractable for the interaction system.
// EDIT (collection-decoupling): collection moved here from ScanController.
// The requiresScan flag controls whether scanning is needed or the item is pickup-only.
public class WorldItem : MonoBehaviour, IScannable, IInteractable
{
    [Header("Item")]
    public ItemDefinition itemDefinition;

    [Tooltip("How many of this item the player receives on pickup.")]
    public int pickupQuantity = 1;
    public bool collectable;

    [Header("Scan")]
    [Tooltip("When true, the item can be scanned (outline, progress bar, snapshot). When false, the item is pickup-only and the scan system ignores it.")]
    public bool requiresScan = true;
    public float scanDuration = 2f;

    [Header("Audio")]
    [SerializeField] private SoundDataSO pickupSound;

    private ScanOutline scanOutline;
    private DiscoveryLog discoveryLog;
    private Inventory inventory;
    private PhotoSnapshots snapshotHandler;

    void Awake()
    {
        scanOutline = GetComponent<ScanOutline>();

        if (discoveryLog == null)
            discoveryLog = FindAnyObjectByType<DiscoveryLog>();
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
        if (snapshotHandler == null)
            snapshotHandler = FindAnyObjectByType<PhotoSnapshots>();
    }

    // -- IScannable --

    public float ScanDuration => scanDuration;

    // EDIT (collection-decoupling): when requiresScan is false, always report as discovered
    // so ScanController never starts a scan bar on this item.
    public bool IsDiscovered
    {
        get
        {
            if (!requiresScan) return true;
            return itemDefinition != null && discoveryLog != null && discoveryLog.HasDiscovered(itemDefinition);
        }
    }

    public bool IsRescannable => false;

    public void OnScanComplete(Texture2D snapshot)
    {
        if (itemDefinition == null || discoveryLog == null) return;
        if (discoveryLog.HasDiscovered(itemDefinition)) return;

        discoveryLog.Add(itemDefinition, snapshot);
    }

    public void SetOutlineVisible(bool visible)
    {
        if (scanOutline != null) scanOutline.SetVisible(visible);
    }

    public void SetOutlineColor(Color color)
    {
        if (scanOutline != null) scanOutline.SetColor(color);
    }

    // -- IInteractable --

    // EDIT (collection-decoupling): collection logic moved here from ScanController.
    public void Interact(InteractionContext context)
    {
        if (!collectable) return;
        if (itemDefinition == null) return;

        // Add to inventory.
        if (inventory != null)
            inventory.Add(itemDefinition, pickupQuantity);

        // Discover on pickup if not already discovered (e.g. picked up without scanning first).
        if (discoveryLog != null && !discoveryLog.HasDiscovered(itemDefinition))
        {
            Texture2D snapshot = snapshotHandler != null ? snapshotHandler.TakeSnapshot() : null;
            discoveryLog.Add(itemDefinition, snapshot);
        }

        if (pickupSound != null)
            AudioManager.PlaySound(pickupSound);

        Debug.Log("Collected " + gameObject.name);
        Destroy(gameObject);
    }

    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        if (!collectable) return InteractionPrompt.None;

        return new InteractionPrompt
        {
            surface = PromptSurface.Hud,
            label = "Pick Up",
            actionName = "Collect",
        };
    }
}
