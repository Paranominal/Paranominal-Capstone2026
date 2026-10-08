using System.Collections;
using UnityEngine;

public class RollingCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RollingColumn[] columns;
    [Tooltip("comma goes here :)")]
    [SerializeField] private RollingColumn[] separators;

    [Header("Animation")]
    [SerializeField] private float duration = 0.6f;
    [Tooltip("how hard the roll decelerates")]
    [Range(1.5f, 4f)]
    [SerializeField] private float easePower = 2f;
    [Tooltip("overshoot distance as a fraction of one symbol on the ones column")]
    [SerializeField] private float nudgeAmount = 0.15f;
    [Tooltip("fraction of the time spent easing back from the overshoot")]
    [Range(0f, 0.5f)]
    [SerializeField] private float settleFraction = 0.2f;

    [Header("Debug")]
    [SerializeField] private int testAmount = 40;

    private float displayedValue;
    private int targetValue;
    private Coroutine rollRoutine;

    public bool IsRolling => rollRoutine != null;

    private void Start()
    {
        ShowValue(displayedValue); // draw once so the columns don't show their editor placeholders
    }

    private void OnDisable()
    {
        // coroutines stop when the object is turned off, so land the roll rather than freezing mid-symbol
        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            FinishRoll();
        }
    }

    public void SetValue(int value, bool animate = true)
    {
        targetValue = value;

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine); // a new roll carries on from wherever the old one got to
            rollRoutine = null;
        }

        if (!animate || !isActiveAndEnabled || duration <= 0f || value == displayedValue) // can't run coroutines on hidden objects, so just snap
        {
            FinishRoll();
            return;
        }

        rollRoutine = StartCoroutine(Roll(displayedValue, value));
    }

    private IEnumerator Roll(float start, float target)
    {
        float overshoot = target + nudgeAmount;
        float travelTime = duration * (1f - settleFraction);
        float settleTime = duration * settleFraction;

        // travel: quick off the mark, slowing to a stop just past the target
        float elapsed = 0f;
        while (elapsed < travelTime)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled so it still runs while time is paused
            float t = Mathf.Clamp01(elapsed / travelTime);
            float eased = 1f - Mathf.Pow(1f - t, easePower);
            ShowValue(Mathf.Lerp(start, overshoot, eased));
            yield return null;
        }

        // settle: nudging back into place
        elapsed = 0f;
        while (elapsed < settleTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settleTime);
            ShowValue(Mathf.Lerp(overshoot, target, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        FinishRoll();
    }

    private void FinishRoll()
    {
        ShowValue(targetValue); // land exactly
        rollRoutine = null;
    }

    private void ShowValue(float value)
    {
        displayedValue = value;

        float placeValue = 1f; // 1, 10, 100...
        for (int k = 0; k < columns.Length; k++)
        {
            float position = Mathf.Floor(value / placeValue); // whole steps this column has taken
            float below = value % placeValue;                 // what the columns to its right show, e.g. 9.4 or 99.4
            if (below > placeValue - 1f)                      // they're all on 9 and rolling over...
                position += below - (placeValue - 1f);        // ...so this column rolls with them

            // a column only takes up space once the number reaches it, opening up as its first digit rolls in
            float presence = k == 0 ? 1f : Mathf.Clamp01(position);

            columns[k].SetPosition(position);
            columns[k].SetPresence(presence);

            // every third column adds a comma with it
            if (k > 0 && k % 3 == 0 && k / 3 - 1 < separators.Length)
            {
                RollingColumn comma = separators[k / 3 - 1];
                comma.SetPosition(0f);
                comma.SetPresence(presence);
            }

            placeValue *= 10f;
        }
    }

    [ContextMenu("Test Add")]
    private void TestAdd()
    {
        SetValue(targetValue + testAmount); // adds to the target, so spamming it stacks like real scoring
    }
}
