using UnityEngine;

public class DistanceScale : MonoBehaviour
{
    [Range(0,1)]
    [SerializeField] private float influence = 1;
    [Range(0.01f,2)]
    [SerializeField] private float finalSizeModifier = 1;
    private Vector3 cachedScale;
    void Awake()
    {
        cachedScale = transform.localScale;
    }

    void Update ()
    {
        float distance = (Camera.main.transform.position - transform.position).magnitude;
        float size = distance * Camera.main.fieldOfView * 0.01f;
        transform.localScale = (cachedScale + Vector3.one * size * influence) * finalSizeModifier;
        transform.forward = transform.position - Camera.main.transform.position;
    }
}
