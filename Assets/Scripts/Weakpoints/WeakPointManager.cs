using UnityEngine;

public class WeakPointManager : MonoBehaviour
{
    [SerializeField] private GameObject[] weakpoints;

    private int currentWeakpoint = 0;
    private EnemyStagger staggerComponent;
    private EnemyKnockback knockbackComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //cache stagger components if they exist
        staggerComponent = GetComponentInParent<EnemyStagger>();
        knockbackComponent = GetComponentInParent<EnemyKnockback>();
        SetupWeakpoints();
    }

    void SetupWeakpoints()
    {
        foreach (GameObject weakpoint in weakpoints)
        {
            weakpoint.GetComponent<WeakPoint>().Hide(); // deactivate all weakpoints
            weakpoint.GetComponent<WeakPoint>().manager = this;// names itself manager within weakpoint scripts
        }
        weakpoints[0].GetComponent<WeakPoint>().Show(weakpoints[0].GetComponent<WeakPoint>().weakPointType); // activate the first weakpoint in the index
    }

    public void NextWeakPoint()
    {
        // weakpoint destroyed: no longer triggers stagger (scan-only)
        currentWeakpoint += 1;
        if (currentWeakpoint < weakpoints.Length)
        {
            weakpoints[currentWeakpoint].GetComponent<WeakPoint>().Show(weakpoints[0].GetComponent<WeakPoint>().weakPointType);
        }
        else
        {
            //checks for miniboss cycles
            ToMiniBoss miniBoss = GetComponentInParent<ToMiniBoss>();
            if (miniBoss != null)
            {
                miniBoss.OnCycleComplete();
            }
            else
            {
                Debug.Log("Enemy Killed!");
                Destroy(gameObject);
            }
        }
    }

    //hitting weakpoints will trigger the kb
    public void OnWeakPointHit()
    {
        if (knockbackComponent != null)
        {
            knockbackComponent.ApplyKnockback();
        }
    }

    //allows miniboss cycles to repeat
    public void ResetWeakpoints()
    {
        currentWeakpoint = 0;
        SetupWeakpoints(); 
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
