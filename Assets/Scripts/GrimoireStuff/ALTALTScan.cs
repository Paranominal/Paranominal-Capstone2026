using UnityEngine;
using UnityEngine.InputSystem;

public class ALTALTScan : MonoBehaviour
{
    public LayerMask scannable;
    InputAction scanAction;
    InputAction collectAction;  // i started implementing collecting as a seperate set of scripts and then realised it was going to either be so wrapped up in this as to be problematic or duplicate so much code it would be incredibly questionable. so. voila.
    private ALTGrimoire grimoire;
    private ALTScannableObject currentTarget;

    public ScanModeVisuals visuals;
    public ScanReticle reticle;

    private bool inScanMode = false;
    private float scanProgress = 0f;
    public float scanDuration = 2f;
    public float scanRange = 20f;


    void Start()
    {
        scanAction = InputSystem.actions.FindAction("Scan");
        collectAction = InputSystem.actions.FindAction("Collect");
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
    }

    void Update()
    {
        if (grimoire.grimoireActive) return; // shouldn't be able to happen bc action maps, but that may change eventually

        bool wantScanMode = scanAction.IsPressed();
        if (wantScanMode != inScanMode)
        {
            SetScanMode(wantScanMode);
        }

        Vector2 mousePos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, scanRange, scannable))
        {
            // Debug.DrawLine(transform.position, hit.point, Color.cyan, 10); // can view this in gizmos mode to help with debugging
            ALTScannableObject target = hit.collider.GetComponent<ALTScannableObject>();
            //Debug.Log("Hit " + tempScan);

            // runs regardless of scan mode
            if (collectAction.WasReleasedThisFrame() && target.collectable)
            {
                // item gets collected here
                if (!grimoire.CompareEntry(target.entry))
                {
                    grimoire.AddEntry(target.entry, true);
                }
                else
                {
                    grimoire.CollectEntry(target.entry);
                }
                Debug.Log("Destroyed " + target.gameObject);
                Destroy(target.gameObject);

                if (target == currentTarget)
                {
                    currentTarget = null;
                    scanProgress = 0f;
                    reticle.SetProgress(0f);
                }
                return; // nothing left to scan this frame
            }

            if (inScanMode)
            {
                if (target != currentTarget)
                {
                    if (currentTarget != null)
                    {
                        visuals.ApplyOutlineColor(currentTarget); // reset previous target's colour if stopped scanning
                    }
                    scanProgress = 0f;
                    currentTarget = target;
                    if (grimoire.CompareEntry(currentTarget.entry)) // opening the previous one if already scanned
                    {
                        grimoire.SelectEntry(grimoire.GetEntryID(currentTarget.entry.entryName));
                    }
                }

                bool alreadyScanned = grimoire.CompareEntry(currentTarget.entry);

                if (!alreadyScanned)
                {
                    scanProgress += Time.deltaTime / scanDuration;
                    scanProgress = Mathf.Clamp01(scanProgress);

                    currentTarget.outline.OutlineColor = Color.Lerp(ScanModeVisuals.GetCategoryColor(currentTarget), Color.white, scanProgress);

                    if (scanProgress >= 1f)
                    {
                        grimoire.AddEntry(currentTarget.entry);
                        visuals.ApplyOutlineColor(currentTarget);
                        scanProgress = 0f;
                    }
                }

                reticle.SetProgress(alreadyScanned ? 0f : scanProgress);
            }
        }
        else
        {
            if (currentTarget != null)
            {
                visuals.ApplyOutlineColor(currentTarget); // reset colour when losing target
                currentTarget = null;
            }
            scanProgress = 0f;
            reticle.SetProgress(0f);
        }
    }

    private void SetScanMode(bool active)
    {
        inScanMode = active;
        visuals.SetScanMode(active);

        if (!active)
        {
            if (currentTarget != null)
            {
                visuals.ApplyOutlineColor(currentTarget);
                currentTarget = null;
            }
            scanProgress = 0f;
            reticle.SetProgress(0f);
        }
    }
}