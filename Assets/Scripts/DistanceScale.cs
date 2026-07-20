using UnityEngine;

public class DistanceScale : MonoBehaviour
{
    public float FixedSize = .01f;

    void Update ()
    {
        var distance = (Camera.main.transform.position - transform.position).magnitude;
        var size = distance * FixedSize * Camera.main.fieldOfView;
        transform.localScale = Vector3.one * size;
        transform.forward = transform.position - Camera.main.transform.position;
    }
}
