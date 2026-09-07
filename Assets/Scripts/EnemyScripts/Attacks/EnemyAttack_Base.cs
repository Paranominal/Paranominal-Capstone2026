using System.Collections;
using UnityEngine;

public class EnemyAttack_Base : MonoBehaviour
{
    public enum AttackState { Ready, WindUp, Attacking,  WindDown,Cooldown }
    public AttackState attackState = AttackState.Ready;
    public LayerMask targetLayers;
    [SerializeField] private int damage = 10;
    public int Damage => damage;
    [Tooltip("Time between each attack in Seconds")]
    [SerializeField] private float coolDownTime = 2f;
    [Tooltip("Wind Up time in Seconds")]
    [SerializeField] private float windUpTime = 1f;
    public float WindUpTime => windUpTime;
    [SerializeField] private SpriteRenderer windupIndicator;
    [Header("Damage Field")]
    [SerializeField] private DamageField damageFieldPrefab;
    public DamageField DamageFieldPrefab => damageFieldPrefab;
    [SerializeField] private float attackRange = 1f;
    public float AttackRange => attackRange;
    [SerializeField] private float damageFieldHeight = 0.5f;
    public float DamageFieldHeight => damageFieldHeight;
    [Tooltip("Number of seconds the Damage Field persists after appearing")]
    [SerializeField] private float damageWindow = 1f;
    public float DamageWindow => damageWindow;
    // [SerializeField] private bool doInterrupt = true;
    // Michael edit (spawn-visual-fix): hide indicator in Awake so it's never visible on spawn.
    void Awake()
    {
        attackState = AttackState.Ready;
        SetWindupIndicator(false);
    }
    public void InitiateAttack(Vector3 targetPos)
    {
        if (attackState != AttackState.Ready) return;
        currentWindUpTime = windUpTime;
        StartCoroutine(WindUp(targetPos));
    }
    [HideInInspector] public float currentWindUpTime;
    public virtual IEnumerator WindUp(Vector3 targetPos)
    {
        attackState = AttackState.WindUp;
        SetWindupIndicator(true);
        // yield return new WaitForSeconds(windUpTime);
        while (currentWindUpTime > 0)
        {
            currentWindUpTime -= Time.deltaTime;
            yield return null;
        }
        DoAttack(targetPos);
        if (currentAttack != null && !currentAttack.attackComplete && !currentAttack.hitRegistered) StartCoroutine(WaitForAttackEnd());
        else DoCooldown();
        SetWindupIndicator(false);
    }
    
    public IEnumerator WaitForAttackEnd()
    {
        attackState = AttackState.WindDown;
        yield return new WaitUntil(() => currentAttack == null || currentAttack.attackComplete);
        DoCooldown();
    }
    float currentCooldownTime;
    public void DoCooldown(float multiplier = 1)
    {
        currentCooldownTime = coolDownTime * multiplier;
        StartCoroutine(Cooldown());
    }
    IEnumerator Cooldown() // additional multipler incase we want interrupts to be more impactful
    {
        attackState = AttackState.Cooldown;
        // yield return new WaitForSeconds(coolDownTime * multiplier);
        while (currentCooldownTime > 0)
        {
            currentCooldownTime -= Time.deltaTime;
            yield return null;
        }
        attackState = AttackState.Ready;
    }
    [HideInInspector] public DamageField currentAttack;
    public virtual void DoAttack(Vector3 targetPos)
    {
        // enemy attack overrides this method
    }
    public void SetWindupIndicator(bool warn)
    {
        if (windupIndicator) windupIndicator.enabled = warn;
    }
    bool isInterrupted;
    public bool IsInterrupted => isInterrupted; 
    public virtual void InterruptAttack()
    {
        StopCoroutine(WindUp(Vector3.zero));
        DoCooldown(2);
        SetWindupIndicator(false);
    }
}
