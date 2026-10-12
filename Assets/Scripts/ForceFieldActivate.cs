using UnityEngine;

public class ForceFieldActivate : MonoBehaviour
{
    [SerializeField]
    private PlayerDash DashScript;

    private bool fieldactive;

    [SerializeField]
    private GameObject child1, child2;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        child1.SetActive(false);
        child2.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!fieldactive && DashScript.dashEnabled)
        {
            child1.SetActive(true);
            child2.SetActive(true);

            fieldactive= true;
        }
    }
}
