using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttacks : MonoBehaviour
{
    PlayerCharacterController pc;
    private bool attackOnCooldownLocal = false; 

    void Start()
    {
        pc = GetComponent<PlayerCharacterController>();
        if (pc == null) Debug.LogWarning("PlayerAttacks: no se encontró PlayerCharacterController en el mismo GameObject.");
    }

    void Update()
    {
        if (pc == null) return;

        if (pc.isAttacking)
        {
            Collider[] attackHits = Physics.OverlapSphere(transform.position, pc.attackRadius);
            foreach (Collider attackHit in attackHits)
            {
                if (attackHit.CompareTag("Box"))
                {
                    pc.DestroyCrateEvent(attackHit.GetComponent<Crate>());
                }
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (pc == null) pc = GetComponent<PlayerCharacterController>();

        if (context.performed)
        {
            if (!pc.isAttacking && !attackOnCooldownLocal)
            {
                StartCoroutine(PerformAttack());
            }
        }
    }

    private IEnumerator PerformAttack()
    {
        if (pc == null) yield break;

        pc.isAttacking = true;
        pc.accumulatedAttackSpinDegrees = 0f;
        if (pc.spinEffectMesh != null)
        {
            pc.spinEffectMesh.SetActive(true);
        }

        yield return new WaitForSeconds(pc.attackDuration);

        pc.isAttacking = false;
        if (pc.spinEffectMesh != null)
        {
            pc.spinEffectMesh.SetActive(false);
        }

        StartCoroutine(AttackCooldown());
    }

    private IEnumerator AttackCooldown()
    {
        attackOnCooldownLocal = true;
        yield return new WaitForSeconds(pc.attackCooldownDuration);
        attackOnCooldownLocal = false;
    }
}
