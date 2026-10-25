using UnityEngine;

public class FloatAndRotate : MonoBehaviour
{
    [Header("Rotate")]
    [SerializeField] private bool doRotate = true;
    [SerializeField, ShowIf("doRotate", true)] private Vector3 rotationSpeedLocal;
    [SerializeField, ShowIf("doRotate", true)] private Vector3 rotationSpeedWorld;
    [Header("Float")]
    [SerializeField] private bool doFloat;
    [SerializeField, ShowIf("doFloat", true)] private bool randomStart = true;
    [SerializeField, ShowIf("doFloat", true), Range(0.01f,10)] private float floatSpeed = 1;
    [SerializeField, ShowIf("doFloat", true)] private Vector3 floatDirection;

    private Vector3 originPos;
    private float floatTime;
    void Awake()
    {
        if (randomStart) floatTime = Random.Range(0, 100);
        originPos = transform.position;
    }

    void Update()
    {
        if (doRotate) Rotate();
        if (doFloat) Float();

        if (!doRotate && !doFloat)
        {
            Debug.LogWarning($"[{this}] is set to not rotate or float! script can probably be removed!");
            enabled = false;
        } 
    }

    void Rotate()
    {
        transform.Rotate(rotationSpeedLocal * Time.unscaledDeltaTime, Space.Self);
        transform.Rotate(rotationSpeedWorld * Time.unscaledDeltaTime, Space.World);
    }

    void Float()
    {
        if (floatTime >= 100f) floatTime -= 100f;
        else floatTime += Time.unscaledDeltaTime * floatSpeed;

        transform.position = originPos + (floatDirection * Mathf.Sin(floatTime));
    }
}
