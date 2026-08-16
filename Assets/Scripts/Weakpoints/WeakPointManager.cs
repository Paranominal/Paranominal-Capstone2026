using UnityEngine;

public class WeakPointManager : MonoBehaviour
{
    public WeakPoint[] weakpoints;
    public int currentWeakpoint = 0;
    [SerializeField] private bool resetSequenceEveryStagger = true;
    public bool debugMode;

    void Start()
    {
        SetupWeakpoints();
    }
    void Update()
    {
        if (weakpoints[currentWeakpoint].hasBeenHit) NextInSequence();
    }
    public void SetupWeakpoints()
    {
        currentWeakpoint = 0;
        foreach (WeakPoint weakpoint in weakpoints)
        {
            weakpoint.hasBeenHit = false;
            weakpoint.SetUpWeakpoint(this); //names itself manager within weakpoint, ands sets up the weakpoint.
        }
        if (debugMode) Debug.Log($"[{this}] Setup Weakpoints: [{weakpoints}] for {gameObject}");
        // weakpoints[0].Show(); // activate the first weakpoint in the index
    }
    public void StartSequence()
    {
        if (resetSequenceEveryStagger) SetupWeakpoints();
        weakpoints[currentWeakpoint].Show(); // activate the first weakpoint in the index
        if (debugMode) Debug.Log($"[{this}] Started Weakpoint Sequence for {gameObject}");
    }
    public void EndSequence()
    {
        // weakpoints[currentWeakpoint].Hide(); // hide current weakpoint
        foreach (WeakPoint weakPoint in weakpoints) weakPoint.Hide(); // hide all weakpoints
        if (debugMode) Debug.Log($"[{this}] Ended Weakpoint Sequence for {gameObject}");
    }
    private void NextInSequence()
    {
        if (debugMode) Debug.Log($"[{this}] Next Weakpoint in sequence on {gameObject} (Weakpoint #{currentWeakpoint + 1})");
        weakpoints[currentWeakpoint].Hide();
        currentWeakpoint += 1;
        if (currentWeakpoint < weakpoints.Length) weakpoints[currentWeakpoint].Show();
        else SequenceComplete();
    }
    private void SequenceComplete() //checks for miniboss cycles
    {
        ToMiniBoss miniBoss = GetComponentInParent<ToMiniBoss>();
        if (miniBoss != null) miniBoss.OnCycleComplete();
        else
        {
            Debug.Log("Enemy Killed!");
            Destroy(gameObject);
        }
    }
    //returns total amount of weakpoints for the enemy
    public int GetTotalWeakpoints()
    {
        return weakpoints.Length;
    }
    public bool UnlockWeakPointById(string weakPointId)
    {
        return WeakPointRegistry.UnlockWardedWeakPointById(weakPointId);

    }
}
