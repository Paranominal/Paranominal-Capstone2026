using Unity.VisualScripting;
using UnityEngine;

public class WeakPointManager : MonoBehaviour
{
    [SerializeField] private GameObject[] weakpoints;
    private int currentWeakpoint = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupWeakpoints();
    }

    // Update is called once per frame
    void SetupWeakpoints()
    {
        foreach (GameObject weakpoint in weakpoints)
        {
            weakpoint.GetComponent<WeakPoint>().Dectivate(); // deactivate all weakpoints
            weakpoint.GetComponent<WeakPoint>().weakPointManager = this;// names itself manager within weakpoint scripts
        }
        weakpoints[0].GetComponent<WeakPoint>().Activate(); // activate the first weakpoint in the index
    }

    public void NextWeakPoint()
    {
        weakpoints[currentWeakpoint].GetComponent<WeakPoint>().Dectivate();
        currentWeakpoint += 1;
        if (currentWeakpoint < weakpoints.Length)
        {
            weakpoints[currentWeakpoint].GetComponent<WeakPoint>().Activate();
        }
        else
        {
            Debug.Log("Enemy Killed!");
            Destroy(gameObject);
        }
    }
}
