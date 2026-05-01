using TMPro;
using UnityEditor.EditorTools;
using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    [SerializeField] private WeakPointManager weakPointsManager;
    [SerializeField] private TextMeshPro display;
    [SerializeField] private int toughness = 1;
    [Tooltip("Enemy HP = Toughness * the enemy's number of weakpoints.")]
    [HideInInspector] public int hp = 1;
    void Start()
    {
        hp = weakPointsManager.GetTotalWeakpoints() * toughness;
    }

    public void IsShot()
    {
        hp--;
        UpdateHPDisplay();
    }
    void UpdateHPDisplay()
    {
        display.text = hp.ToString();
    }
}
