using TMPro;
using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    [SerializeField] private WeakPointManager weakPointsManager;
    [SerializeField] private TextMeshPro display;
    [Tooltip("Enemy HP = Toughness * the enemy's number of weakpoints. Weakpoints ignore Toughness!")]
    [SerializeField] private int toughness = 1;
    [HideInInspector] public int hp = 1;
    private int damageTaken = 0;
    private int hpMax;
    void Start()
    {
        hp = weakPointsManager.GetTotalWeakpoints() * toughness;
        hpMax = hp;
        weakPointsManager.enemyHPComponent = this;
    }
    void Update()
    {
        Debug.Log("hp" + hp);
        Debug.Log("hpMax" + hpMax);
        Debug.Log("damageTaken" + damageTaken);
    }
    public void IsShot()
    {
        hp--;
        damageTaken++;
        UpdateHPDisplay();


        if (hp <= 0)
        {
            Debug.Log("enemy " + gameObject + " was killed!");
            Destroy(gameObject);
        }
    }
    public void WeakpointIsShot()
    {
        RecalculateHP();
        UpdateHPDisplay();

        if (hp <= 0)
        {
            Debug.Log("enemy " + gameObject + " was killed!");
            Destroy(gameObject);
        }
    }
    public void RecalculateHP()
    {
        hp = hpMax - damageTaken - (weakPointsManager.currentWeakpoint * toughness);
        UpdateHPDisplay();
    }
    void UpdateHPDisplay()
    {
        display.text = hp.ToString();
    }
}
