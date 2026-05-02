using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ALTALTScan : MonoBehaviour
{
    public LayerMask scannable;
    public LayerMask enemyLayer;
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
    public float cooldownTimer = 0.5f;
    private bool isOnCooldown;


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
        if (isOnCooldown) return;

        bool wantScanMode = scanAction.IsPressed();
        if (wantScanMode != inScanMode)
        {
            SetScanMode(wantScanMode);
        }

        if (!inScanMode) return;

        Vector2 mousePos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, scanRange, scannable))
        {
            // Debug.DrawLine(transform.position, hit.point, Color.cyan, 10); // can view this in gizmos mode to help with debugging
            ALTScannableObject target = hit.collider.GetComponent<ALTScannableObject>();
            //Debug.Log("Hit " + tempScan);
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

            //if (!alreadyScanned)
            //{
                scanProgress += Time.deltaTime / scanDuration;
                scanProgress = Mathf.Clamp01(scanProgress);

                currentTarget.outline.OutlineColor = Color.Lerp(ScanModeVisuals.GetCategoryColor(currentTarget), Color.white, scanProgress);

                if (scanProgress >= 1f)
                {
                    grimoire.AddEntry(currentTarget.entry);
                    visuals.ApplyOutlineColor(currentTarget);
                    scanProgress = 0f;
                    
                    
                    if (Physics.Raycast(ray, out RaycastHit enemy, 5f, enemyLayer))
                    {
                        Debug.Log("Scan hit " + enemy.collider + " with Stun");
                        enemy.collider.GetComponent<EnemyStagger>().TriggerStagger(); //stagger enemy if its an enemy

                    StartCoroutine(ScanCooldown());
                    }
                }
            //}

            //reticle.SetProgress(alreadyScanned ? 0f : scanProgress);
            reticle.SetProgress(scanProgress);

            if (collectAction.WasReleasedThisFrame() && currentTarget.collectable)
            {
                // item gets collected here
                if (!grimoire.CompareEntry(currentTarget.entry))   //checks if the entry for the collected item has been scanned and adds it if not
                {
                    grimoire.AddEntry(currentTarget.entry, true);
                }
                else
                {
                    grimoire.CollectEntry(currentTarget.entry);
                }
                Debug.Log("Destroyed " + currentTarget.gameObject);
                Destroy(currentTarget.gameObject);
                currentTarget = null;
                scanProgress = 0f;
                reticle.SetProgress(0f);
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

    private IEnumerator ScanCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTimer);
        isOnCooldown = false;
    }
}