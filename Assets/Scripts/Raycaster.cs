using UnityEngine;
using UnityEngine.InputSystem;

public class Raycaster : MonoBehaviour
{
    private Ray ray;
    static Raycaster instance;

    public Ray Ray
    {
        get { return ray; }
    }
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        if (instance != this)
        {
            Debug.LogError("Multiple Raycasters found in the scene.");
        }
    }


    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());
    }
}
