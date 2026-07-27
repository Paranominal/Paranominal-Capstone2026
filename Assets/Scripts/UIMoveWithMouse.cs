using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIMoveWithMouse : MonoBehaviour
{
    [SerializeField] private float rotationStrength = 1;
    private Vector2 mousePos;
    private Quaternion cachedRotation;
    void Start()
    {
        cachedRotation = transform.rotation;
    }
    void Update()
    {
        mousePos = GetViewportPosition();

        transform.rotation = Quaternion.Euler(cachedRotation.eulerAngles + (new Vector3(mousePos.y, mousePos.x * -1, 0) * rotationStrength));
    }
    private float2 GetViewportPosition()
    {
        float2 screenPos = GetBoundedScreenPosition();
        float3 viewportPos = Camera.main.ScreenToViewportPoint( new float3(screenPos, 0));
        return (viewportPos.xy - 0.5f) * 2;
    }

    private float2 GetBoundedScreenPosition()
    {
        return math.clamp(Mouse.current.position.ReadValue(), float2.zero, new float2(Screen.width, Screen.height));
    }
}
