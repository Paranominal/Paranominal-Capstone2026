using UnityEngine;

public class LightMover : MonoBehaviour
{
    public float speed = 2f;
    private Vector3 startPosition;
    private float targetY;

    void Start()
    {
        startPosition = transform.position;
        targetY = startPosition.y - 4f;
    }

    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
        if (transform.position.y <= targetY)
        {
            transform.position = startPosition;
        }
    }
}