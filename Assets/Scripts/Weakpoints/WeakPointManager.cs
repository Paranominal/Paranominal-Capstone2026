using Unity.VisualScripting;
using UnityEngine;

public class WeakPointManager : MonoBehaviour
{
    [SerializeField] private GameObject[] weakpoints;
    private int currentWeakpoint = 0;
    private EnemyStagger staggerComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Cache the stagger component if it exists
        staggerComponent = GetComponentInParent<EnemyStagger>();
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
        weakpoints[0].GetComponent<WeakPoint>().Show(weakpoints[0].GetComponent<WeakPoint>().weakPointType); // activate the first weakpoint in the index
    }

    public void NextWeakPoint()
    {
        //nak was here
        //notify stagger script that weeakpoint is destroyed
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
    //returns total amount of weakpoints for the enemy
    public int GetTotalWeakpoints()
    {
        return weakpoints.Length;

    public bool UnlockWeakPointById(string weakPointId)
    {
        return WeakPointRegistry.UnlockWardedWeakPointById(weakPointId);

    }
}
