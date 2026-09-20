using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] private string dashEntryName;
    private bool dashObtained;

    void Update()
    {
        if (!dashObtained && grimoire.entries.Exists(dashEntry => dashEntry.entryName == dashEntryName)) GainDash();
    }

    void GainDash()
    {
        playerMover.dashEnabled = true;
        dashObtained = true;
    }
}
