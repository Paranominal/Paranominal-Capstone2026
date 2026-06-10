using UnityEngine;

public class ElevatorShake : MonoBehaviour
{
    public float magnitude = 0.03f;
    public float speed = 15f;

    private Vector3 initialLocalPosition;

    void Start()
    {
        initialLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        float offsetX = (Mathf.PerlinNoise(Time.time * speed, 0f) - 0.5f) * 2f * magnitude;
        float offsetY = (Mathf.PerlinNoise(0f, Time.time * speed) - 0.5f) * 2f * magnitude;

        transform.localPosition = initialLocalPosition + new Vector3(offsetX, offsetY, 0f);
    }
}