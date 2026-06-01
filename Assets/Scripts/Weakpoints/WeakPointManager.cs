using UnityEngine;

public class WeakPointManager : MonoBehaviour
{
    [SerializeField] private GameObject[] weakpoints;

    [HideInInspector] public int currentWeakpoint = 0;
    private EnemyStagger staggerComponent;
    [HideInInspector] public EnemyHP enemyHPComponent;
    private EnemyKnockback knockbackComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //cache stagger components if they exist
        staggerComponent = GetComponentInParent<EnemyStagger>();
        knockbackComponent = GetComponentInParent<EnemyKnockback>();
        SetupWeakpoints();
    }

    bool swap = true;
    void Update()
    {
        if (staggerComponent.IsStaggered && swap == true)
        {
            ExposeWeakpoints();
            swap = false;
        } 
        else if (!staggerComponent.IsStaggered && swap == false)
        {
            HideCurrentWeakpoint();
            swap = true;
        } 
    }
    void SetupWeakpoints()
    {
        foreach (GameObject weakpoint in weakpoints)
        {
            weakpoint.GetComponent<WeakPoint>().Hide(); // deactivate all weakpoints
            weakpoint.GetComponent<WeakPoint>().manager = this;// names itself manager within weakpoint scripts
        }
        //weakpoints[0].GetComponent<WeakPoint>().Show(weakpoints[0].GetComponent<WeakPoint>().weakPointType); // activate the first weakpoint in the index
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
            Debug.Log("Enemy Killed!");
            Destroy(gameObject);
        }
    }

    //nak was here
    //hitting weakpoints will trigger the kb
    public void OnWeakPointHit()
    {
        if (knockbackComponent != null)
        {
            knockbackComponent.ApplyKnockback();
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
    public void ExposeWeakpoints()
    {
        foreach (GameObject weakpoint in weakpoints)
        {
            weakpoint.GetComponent<WeakPoint>().Hide(); // hide all weakpoints
        }
        if (staggerComponent.IsStaggered) weakpoints[currentWeakpoint].GetComponent<WeakPoint>().Show(weakpoints[currentWeakpoint].GetComponent<WeakPoint>().weakPointType); // activate the current weakpoint in the index
    }
    public void HideCurrentWeakpoint()
    {
        weakpoints[currentWeakpoint].GetComponent<WeakPoint>().Hide(); // hide current weakpoints
    }
}
