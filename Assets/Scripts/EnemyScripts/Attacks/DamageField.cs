using UnityEngine;

public class DamageField : MonoBehaviour
{
    [HideInInspector] public LayerMask targetLayers;
    private bool hitRegistered;
    void OnTriggerEnter(Collider other)
    {
        if (!hitRegistered && targetLayers.Contains(other.gameObject.layer)) Debug.Log($"[{this}] Hit registered against [{other.gameObject}]!");
        hitRegistered = true;
    }
}
