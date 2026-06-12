using UnityEngine;

public class dimTorchlight : MonoBehaviour
{

    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private float dimFactor = 1f;
    private float cachedIntensity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cachedIntensity = this.gameObject.GetComponent<Light>().intensity;
    }

    // Update is called once per frame
    void Update()
    {
        if (grimoire.grimoireActive) this.gameObject.GetComponent<Light>().intensity = cachedIntensity * dimFactor;
        else this.gameObject.GetComponent<Light>().intensity = cachedIntensity; 
    }
}
