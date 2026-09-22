using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private string dashEntryName;
    private bool dashObtained;

    void Update()
    {
        if (!dashObtained && grimoire.entries.Exists(dashEntry => dashEntry.entryName == dashEntryName)) GainDash();
    }

    void GainDash()
    {
        playerDash.dashEnabled = true;
        playerDash.DashVersionEnabled("charges");
        dashObtained = true;
    }
}
