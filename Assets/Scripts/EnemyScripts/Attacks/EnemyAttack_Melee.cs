using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAttack_Melee : MonoBehaviour
{
    [SerializeField] private GameObject damageFieldPrefab;
    [SerializeField] private SpriteRenderer attackIndicator;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private float windUpSeconds = 1f;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private float damageFieldRadius = 1f;
    [SerializeField] private bool doInterrupt = true;
    public bool windingUp;
    public void DoAttack()
    {
        DoWindUp();
    }
    private void DoWindUp()
    {
        StartCoroutine(WindUp());
    }
    private void DoHit()
    {
        Instantiate(damageFieldPrefab, transform.position + (transform.forward * attackDistance), Quaternion.identity);
    }
    IEnumerator WindUp()
    {
        windingUp = true;
        yield return new WaitForSeconds(windUpSeconds);
        windingUp = false;
        DoHit();
    }
    
}
