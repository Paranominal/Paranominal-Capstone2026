using UnityEngine;

public class ScannableObject : MonoBehaviour
{
    [SerializeField] private GrimoireEntry grimoireEntry;
    [SerializeField] private GrimoireCodex grimoireCodex;

    public void OnScan()
    {
        UpdateGrimoire(); 
    }
    void UpdateGrimoire()
    {
        Debug.Log(grimoireEntry + " was sent to Grimoire.");
        Grimoire.Instance.AddEntry(grimoireEntry);
    }
}
