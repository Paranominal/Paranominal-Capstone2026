using UnityEngine;

// Summary: Marks this GameObject to persist across scene loads.
public class PersistentObject : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}