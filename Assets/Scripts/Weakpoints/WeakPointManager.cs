using UnityEngine;

public class WeakPointManager : MonoBehaviour
{
    [SerializeField] private GameObject[] weakpoints;

    private int currentWeakpoint = 0;
    private EnemyStagger staggerComponent;
    private EnemyStaggerV2 staggerComponentV2;
    private EnemyKnockback knockbackComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //cache stagger components if they exist
        staggerComponent = GetComponentInParent<EnemyStagger>();
        staggerComponentV2 = GetComponentInParent<EnemyStaggerV2>();
        knockbackComponent = GetComponentInParent<EnemyKnockback>();
        SetupWeakpoints();
    }

    // Update is called once per frame
    void SetupWeakpoints()
    {
        foreach (GameObject weakpoint in weakpoints)
        {
            weakpoint.GetComponent<WeakPoint>().Hide(); // deactivate all weakpoints
            weakpoint.GetComponent<WeakPoint>().manager = this;// names itself manager within weakpoint scripts
        }
        if (staggerComponent.IsStaggered) weakpoints[0].GetComponent<WeakPoint>().Show(weakpoints[0].GetComponent<WeakPoint>().weakPointType); // activate the first weakpoint in the index
    }

    public void NextWeakPoint()
    {
        // notify stagger scripts that weakpoint is destroyed
        if (staggerComponent != null)
        {
            staggerComponent.OnWeakPointDestroyed();
        }

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

        //staggerv2 staggers on every weakpoint hit
        if (staggerComponentV2 != null)
        {
            staggerComponentV2.OnWeakPointHit();
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

    public void ScanStun()
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
