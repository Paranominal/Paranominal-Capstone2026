using UnityEngine;
// EDIT (special-shot): needed to build the runtime sequence.
using System.Collections.Generic;

public class WeakPointManager : MonoBehaviour
{
    [SerializeField] private bool resetSequenceEveryStagger = false;
    [SerializeField] private bool alwaysShowAll;
    public WeakPoint[] weakpoints;
    // EDIT (special-shot): runtime order used for the sequence. Same as weakpoints, but Special weakpoints are moved to the end
    // so they're only revealed once everything else is destroyed. Designers don't need to order them manually.
    private WeakPoint[] sequence;
    private int currentWeakpoint = 0;
    public int CurrentWeakpoint => currentWeakpoint;
    public bool debugMode;
    [HideInInspector] public bool dieOnWeakpointsComplete = true;
    private int cyclesComplete = 0;
    public int CyclesComplete => cyclesComplete;
    [HideInInspector] public bool handleOwnDestruction = true;

    void Start()
    {
        SetupWeakpoints();
    }

    //sanity check so that weakpoints are actually a component that is usable
    private bool HasWeakpoints()
    {
        return weakpoints != null && weakpoints.Length > 0;
    }

    // EDIT (special-shot): checks against the runtime sequence instead of the inspector array.
    private bool CurrentIndexValid()
    {
        return sequence != null && currentWeakpoint >= 0 && currentWeakpoint < sequence.Length;
    }

    public void SetupWeakpoints()
    {
        currentWeakpoint = 0;

        if (!HasWeakpoints()) return;

        // EDIT (special-shot): rebuild the runtime order each setup.
        BuildSequence();

        foreach (WeakPoint weakpoint in weakpoints)
        {
            weakpoint.hasBeenHit = false;
            weakpoint.SetUpWeakpoint(this); //names itself manager within weakpoint, ands sets up the weakpoint.
        }
        if (debugMode) Debug.Log($"[{this}] Setup Weakpoints: [{weakpoints}] for {gameObject}");
        // weakpoints[0].Show(); // activate the first weakpoint in the index
        if (alwaysShowAll) StartSequence();
    }

    // EDIT (special-shot): non-Special weakpoints keep their inspector order, Special weakpoints go last.
    private void BuildSequence()
    {
        List<WeakPoint> ordered = new List<WeakPoint>(weakpoints.Length);
        foreach (WeakPoint weakpoint in weakpoints)
            if (weakpoint != null && !weakpoint.IsSpecial) ordered.Add(weakpoint);
        foreach (WeakPoint weakpoint in weakpoints)
            if (weakpoint != null && weakpoint.IsSpecial) ordered.Add(weakpoint);

        sequence = ordered.ToArray();
    }

    public void StartSequence()
    {
        if (!HasWeakpoints()) return;

        if (resetSequenceEveryStagger) SetupWeakpoints();
        // EDIT (special-shot): guard against starting a sequence that's already complete or not set up.
        if (!CurrentIndexValid()) return;

        // EDIT (special-shot): alwaysShowAll now only shows unresolved weakpoints, holding Special ones back until the rest are done.
        if (alwaysShowAll) ShowAvailable();
        else sequence[currentWeakpoint].Show(); // activate the first weakpoint in the index
        if (debugMode) Debug.Log($"[{this}] Started Weakpoint Sequence for {gameObject}");
    }

    // EDIT (special-shot): alwaysShowAll reveal. Shows every unresolved non-Special weakpoint, or the Special ones once
    // all non-Special weakpoints are resolved. Skips anything already visible so tough hit counts and fades aren't reset.
    private void ShowAvailable()
    {
        bool showSpecials = AllNonSpecialsResolved();

        foreach (WeakPoint weakpoint in sequence)
        {
            if (weakpoint == null || weakpoint.hasBeenHit || weakpoint.IsShown) continue;
            if (weakpoint.IsSpecial != showSpecials) continue;
            weakpoint.Show();
        }
    }

    // EDIT (special-shot): true once every non-Special weakpoint has been destroyed (or if there are none).
    private bool AllNonSpecialsResolved()
    {
        foreach (WeakPoint weakpoint in sequence)
        {
            if (weakpoint != null && !weakpoint.IsSpecial && !weakpoint.hasBeenHit)
                return false;
        }
        return true;
    }

    public void EndSequence()
    {
        if (!HasWeakpoints()) return;

        // weakpoints[currentWeakpoint].Hide(); // hide current weakpoint
        foreach (WeakPoint weakPoint in weakpoints) weakPoint.Hide(); // hide all weakpoints
        if (debugMode) Debug.Log($"[{this}] Ended Weakpoint Sequence for {gameObject}");
    }

    // EDIT (special-shot): uses the runtime sequence, and alwaysShowAll reveals Special weakpoints when they're due.
    private void NextInSequence()
    {
        if (debugMode) Debug.Log($"[{this}] Next Weakpoint in sequence on {gameObject} (Weakpoint #{currentWeakpoint + 1})");
        sequence[currentWeakpoint].Hide();
        currentWeakpoint += 1;
        if (currentWeakpoint < sequence.Length)
        {
            if (alwaysShowAll) ShowAvailable();
            else sequence[currentWeakpoint].Show();
        }
        else SequenceComplete();
    }

    // Called by a weakpoint after its hit VFX has finished. Completed weakpoints
    // are consumed in sequence order, including queued hits when all are visible.
    public void NotifyWeakPointResolved(WeakPoint weakpoint)
    {
        if (!HasWeakpoints() || weakpoint == null) return;

        bool belongsToManager = false;
        foreach (WeakPoint managedWeakpoint in weakpoints)
        {
            if (managedWeakpoint == weakpoint)
            {
                belongsToManager = true;
                break;
            }
        }

        if (!belongsToManager) return;

        // EDIT (special-shot): walks the runtime sequence instead of the inspector array.
        while (CurrentIndexValid() && sequence[currentWeakpoint] != null && sequence[currentWeakpoint].hasBeenHit)
            NextInSequence();
    }

    private void SequenceComplete() //checks for miniboss cycles
    {
        cyclesComplete++; 
        if (!dieOnWeakpointsComplete) SetupWeakpoints();
        else if (handleOwnDestruction)
        {
            Debug.Log($"[{this}] was killed!");
            Destroy(gameObject);
        }
    }

    //returns total amount of weakpoints for the enemy
    public int GetTotalWeakpoints()
    {
        return HasWeakpoints() ? weakpoints.Length : 0;
    }
    
    public bool UnlockWeakPointById(string weakPointId)
    {
        return WeakPointRegistry.UnlockWardedWeakPointById(weakPointId);

    }
}
