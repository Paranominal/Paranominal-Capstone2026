using Unity.VisualScripting;
using UnityEngine;

public class RotateEffect : MonoBehaviour
{
    [Header("Local")]
    [SerializeField] private float xSpeed;
    [SerializeField] private float ySpeed;
    [SerializeField] private float zSpeed;
    [Header("World")]
    [SerializeField] private float worldXSpeed;
    [SerializeField] private float worldYSpeed;
    [SerializeField] private float worldZSpeed;
    void Update()
    {
        Rotate();
    }

    void Rotate()
    {
        transform.Rotate(xSpeed * Time.deltaTime, ySpeed * Time.deltaTime, zSpeed * Time.deltaTime, Space.Self);
        transform.Rotate(worldXSpeed * Time.deltaTime, worldYSpeed * Time.deltaTime, worldZSpeed * Time.deltaTime, Space.World);
    }
}
