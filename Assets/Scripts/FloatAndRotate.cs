using System;
using Unity.VisualScripting;
using UnityEngine;

public class FloatAndRotate : MonoBehaviour
{
    [Header("Rotate")]
    [SerializeField] private float rotateLocalXSpeed;
    [SerializeField] private float rotateLocalYSpeed;
    [SerializeField] private float rotateLocalZSpeed;
    [SerializeField] private float rotateWorldXSpeed;
    [SerializeField] private float rotateWorldYSpeed;
    [SerializeField] private float rotateWorldZSpeed;
    [Header("Float")]
    [SerializeField] private float floatLocalXAmplitude;
    [SerializeField] private float floatLocalXFrequency;
    [SerializeField] private float floatLocalYAmplitude;
    [SerializeField] private float floatLocalYFrequency;
    [SerializeField] private float floatLocalZAmplitude;
    [SerializeField] private float floatLocalZFrequency;
    [SerializeField] private float floatWorldXAmplitude;
    [SerializeField] private float floatWorldXFrequency;
    [SerializeField] private float floatWorldYAmplitude;
    [SerializeField] private float floatWorldYFrequency;
    [SerializeField] private float floatWorldZAmplitude;
    [SerializeField] private float floatWorldZFrequency;
    void Update()
    {
        Float();
        Rotate(); 
    }

    void Rotate()
    {
        transform.Rotate(rotateLocalXSpeed * Time.deltaTime, rotateLocalYSpeed * Time.deltaTime, rotateLocalZSpeed * Time.deltaTime, Space.Self);
        transform.Rotate(rotateWorldXSpeed * Time.deltaTime, rotateWorldYSpeed * Time.deltaTime, rotateWorldZSpeed * Time.deltaTime, Space.World);
    }
    float xLocalTime;
    float yLocalTime;
    float zLocalTime;
    float xWorldTime;
    float yWorldTime;
    float zWorldTime;
    void Float()
    {
        xLocalTime += Time.deltaTime * floatLocalXFrequency;
        yLocalTime += Time.deltaTime * floatLocalYFrequency;
        zLocalTime += Time.deltaTime * floatLocalZFrequency;
        xWorldTime += Time.deltaTime * floatWorldXFrequency;
        yWorldTime += Time.deltaTime * floatWorldYFrequency;
        zWorldTime += Time.deltaTime * floatWorldZFrequency;
        transform.localPosition += new Vector3(Mathf.Sin(xLocalTime) * floatLocalXAmplitude / 1000, 0, 0);
        transform.localPosition += new Vector3(0, Mathf.Sin(yLocalTime) * floatLocalYAmplitude / 1000, 0);
        transform.localPosition += new Vector3(0, 0, Mathf.Sin(zLocalTime) * floatLocalZAmplitude / 1000);
        transform.position += new Vector3(Mathf.Sin(xWorldTime) * floatWorldXAmplitude / 1000, 0, 0);
        transform.position += new Vector3(0, Mathf.Sin(yWorldTime) * floatWorldYAmplitude / 1000, 0);
        transform.position += new Vector3(0, 0, Mathf.Sin(zWorldTime) * floatWorldZAmplitude / 1000);
    }
}
