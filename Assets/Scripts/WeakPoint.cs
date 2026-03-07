using UnityEngine;

public class WeakPoint : MonoBehaviour
{
    [SerializeField] GameObject[] weakpoints;
    [SerializeField] private GameObject[] graphics;
    public WeakPointManager weakPointManager;
    private int currentWeakpoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        
    }

    public void Activate()
    {
        gameObject.GetComponent<SphereCollider>().enabled = true;
        foreach (GameObject graphic in graphics)
        {
        graphic.GetComponent<SpriteRenderer>().enabled = true;
        }
    }
    public void Dectivate()
    {
        gameObject.GetComponent<SphereCollider>().enabled = false;
        foreach (GameObject graphic in graphics)
        {
            graphic.GetComponent<SpriteRenderer>().enabled = false;
        }
    }
    
    public void OnHit()
    {
        weakPointManager.NextWeakPoint();
    }
}
