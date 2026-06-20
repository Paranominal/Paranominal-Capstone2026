using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    public float speed = 1;
    void Update()
    {
        transform.Translate(Vector3.forward * Time.deltaTime * speed);
    }
}
