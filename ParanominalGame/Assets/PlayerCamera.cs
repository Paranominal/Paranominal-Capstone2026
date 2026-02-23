using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Camera playerCamera;
    public float walkSpeed = 10f;
    public float camSensX = 1;
    public float camSensY = 1;
    private float xRotation;
    private float yRotation;
    private Transform orientation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        CameraRotate();
    }
    void CameraRotate()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * camSensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * camSensX;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90, 90);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
