using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Summary: Forces all UI Image and TMP_Text components on this object to render
// over scene geometry by setting ZTest to Always. Drop on the label prefab root.
public class WorldLabelOverlay : MonoBehaviour
{
    void Start()
    {
        foreach (var img in GetComponentsInChildren<Image>())
        {
            img.material = new Material(img.material);
            img.material.SetInt("unity_GUIZTestMode", 8);
        }

        foreach (var tmp in GetComponentsInChildren<TMP_Text>())
        {
            tmp.fontMaterial = new Material(tmp.fontMaterial);
            tmp.fontMaterial.SetInt("unity_GUIZTestMode", 8);
        }
    }
}
