using UnityEngine;

public class WeakPoint : MonoBehaviour
{
    [SerializeField] GameObject[] weakpoints;
    [SerializeField] private GameObject graphic;
    public WeakPointManager weakPointManager;
    private int currentWeakpoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (GameObject weakpoint in weakpoints)
        {
        }
    }

    public void Activate()
    {
        gameObject.GetComponent<SphereCollider>().enabled = true;
        graphic.GetComponent<SpriteRenderer>().enabled = true;
    }
    public void Dectivate()
    {
        gameObject.GetComponent<SphereCollider>().enabled = false;
        graphic.GetComponent<SpriteRenderer>().enabled = false;
    }
    
    public void OnHit()
    {
        weakPointManager.NextWeakPoint();
    }
}
