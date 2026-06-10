using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponEvents weaponEvents;

    [Header("Scoring")]
    [SerializeField] private int pointsPerWeakpointHit = 10;

    public int currentScore = 100; // if changing, also change private float visibleSpirit = 100f; in SpiritBar

    public event System.Action<int> OnPointsAdded;

    private void Awake()
    {
        if (weaponEvents == null)
        {
            weaponEvents = GetComponent<WeaponEvents>(); // getting the weapon events from the current object (assuming Player) if not manually assigned
        }
        weaponEvents.ShotResolved += HandleShotResolved;
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentScore += amount;
        Debug.Log($"Final score: {currentScore}"); // printing this in console for now, will be displayed on end screen
        OnPointsAdded?.Invoke(currentScore);
    }

    private void HandleShotResolved(WeakPointType shotType, bool rewarded)
    {
        if (rewarded)
        {
            AddScore(pointsPerWeakpointHit); // handling this in here, but the more scoring additions we add the less it will make sense
        }
    }
}
