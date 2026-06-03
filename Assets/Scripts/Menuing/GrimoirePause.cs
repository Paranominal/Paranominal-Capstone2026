using UnityEngine;

public class GrimoirePause : MonoBehaviour
{
    [SerializeField] private ALTGrimoire grimoire;

    private bool wasGrimoireActive;

    private void Awake()
    {
        if (grimoire == null)
        {
            grimoire = ALTGrimoire.instance;
        }
    }

    private void Update()
    {
        if (grimoire == null)
        {
            return;
        }

        bool isActive = grimoire.grimoireActive;
        if (isActive == wasGrimoireActive)
        {
            return;
        }

        Time.timeScale = isActive ? 0f : 1f;
        wasGrimoireActive = isActive;
    }
}
