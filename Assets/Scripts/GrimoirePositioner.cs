using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

/// <summary>
/// This script is designed to lower the grimoire whenever the player is walking or shooting, to keep it out of your face
/// It is dependent on the GrimoireAnimManager component, it slots into that!
/// </summary>
[RequireComponent(typeof(GrimoireAnimManager))]
public class GrimoirePositioner : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private GameObject handsObject;
    [SerializeField] private float speed = 1f;
    private Vector3 originPosition;
    private Vector3 originPositionHands;
    private Quaternion originRotation;
    [SerializeField] private Transform lowPoint;
    [SerializeField] private Transform lowPointHands;
    [SerializeField] private Transform openPoint;
    [SerializeField] private Transform menuPoint;
    public bool debugMode;
    public enum GrimoirePosition { origin, lowered, open, menu }
    public GrimoirePosition position;
    void Start()
    {
        originPosition = targetObject.transform.localPosition;
        originRotation = targetObject.transform.localRotation;
        
        if (handsObject != null) originPositionHands = handsObject.transform.localPosition;
    }
    // void Update()
    // {
    //     Debug.Log($"[{this}] targetObject.transform.localPosition: {targetObject.transform.localPosition}");
    // }
    public void Lower()
    {
        if (debugMode) Debug.Log($"[{this}] Lowering!");
        position = GrimoirePosition.lowered;
        targetObject.transform.localPosition = Vector3.Lerp(targetObject.transform.localPosition, lowPoint.localPosition, speed * Time.deltaTime);
        targetObject.transform.localRotation = Quaternion.Slerp(targetObject.transform.localRotation, lowPoint.localRotation, speed * Time.deltaTime);

        if (handsObject != null) handsObject.transform.localPosition = Vector3.Lerp(handsObject.transform.localPosition, originPositionHands, speed * Time.deltaTime);
    }
    public void Open()
    {
        if (debugMode) Debug.Log($"[{this}] Opening!");
        position = GrimoirePosition.open;
        targetObject.transform.localPosition = Vector3.Lerp(targetObject.transform.localPosition, openPoint.localPosition, speed * Time.deltaTime);
        targetObject.transform.localRotation = Quaternion.Slerp(targetObject.transform.localRotation, openPoint.localRotation, speed * Time.deltaTime);

        if (handsObject != null) handsObject.transform.localPosition = Vector3.Lerp(handsObject.transform.localPosition, lowPointHands.localPosition, speed * Time.deltaTime);
    }
    public void Raise()
    {
        if (debugMode) Debug.Log($"[{this}] Raising!");
        position = GrimoirePosition.origin;
        targetObject.transform.localPosition = Vector3.Lerp(targetObject.transform.localPosition, originPosition, speed * Time.deltaTime);
        targetObject.transform.localRotation = Quaternion.Slerp(targetObject.transform.localRotation, originRotation, speed * Time.deltaTime);
    }
    public void Menu()
    {
        if (debugMode) Debug.Log($"[{this}] Menuing!");
        position = GrimoirePosition.menu;
        targetObject.transform.localPosition = Vector3.Lerp(targetObject.transform.localPosition, menuPoint.localPosition, speed * Time.unscaledDeltaTime);
        targetObject.transform.localRotation = Quaternion.Slerp(targetObject.transform.localRotation, menuPoint.localRotation, speed * Time.unscaledDeltaTime);
    }
}
