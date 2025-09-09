using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerAttacks : MonoBehaviour
{
    PlayerCharacterController pc;
    private bool attackOnCooldownLocal = false;

    private Collider[] attackHits = new Collider[10];
    private Collider[] bounceHits = new Collider[5];

    void Awake()
    {
        pc = GetComponent<PlayerCharacterController>();
        if (pc == null) Debug.LogWarning("PlayerAttacks: no se encontró PlayerCharacterController en el mismo GameObject.");
    }

    void Update()
    {
        if (pc == null) return;
        
        if (pc.isAttacking)
        {
            Vector3 characterAttackPosition = transform.position + Vector3.down * 0.5f;
            DrawDebugSphere(characterAttackPosition, pc.attackRadius, Color.red);

            int attackHitsSize = Physics.OverlapSphereNonAlloc(characterAttackPosition, pc.attackRadius, attackHits);

            
            for (int i = 0; i < attackHitsSize; i++)
            {
                if (attackHits[i].CompareTag("Box"))
                {
                    attackHits[i].GetComponent<Crate>().OnDestroyCrate(pc);
                }
            }

            /* //Versión no optima de OverlapSphere de referencia
            Collider[] attackHits = Physics.OverlapSphere(transform.position, pc.attackRadius);
            foreach (Collider attackHit in attackHits)
            {
                if (attackHit.CompareTag("Box"))
                {
                    pc.DestroyCrateEvent(attackHit.GetComponent<Crate>());
                }
            }
            */
        }
        else if (!pc.characterController.isGrounded)
        {
            if(pc.velocity.y < 0)
            {
                Vector3 characterFeetPosition = transform.position + pc.characterController.center + Vector3.down * (pc.characterController.height * 0.5f + 0.05f);
                DrawDebugSphere(characterFeetPosition, pc.characterController.radius * 0.5f, Color.blue);

                int bounceHitsSize = Physics.OverlapSphereNonAlloc(characterFeetPosition, pc.characterController.radius * 0.9f, bounceHits);

                for (int i = 0; i < bounceHitsSize; i++)
                {
                    if (bounceHits[i].CompareTag("Box"))
                    {
                        bounceHits[i].GetComponent<Crate>().OnPlayerBounce(pc);
                    }
                }
            }
            else if(pc.velocity.y > 0)
            {
                Vector3 characterHeadPosition = transform.position + pc.characterController.center + Vector3.up * (pc.characterController.height * 0.5f + 0.05f);
                DrawDebugSphere(characterHeadPosition, pc.characterController.radius * 0.5f, Color.blue);

                int bounceHitsSize = Physics.OverlapSphereNonAlloc(characterHeadPosition, pc.characterController.radius * 0.9f, bounceHits);

                for (int i = 0; i < bounceHitsSize; i++)
                {
                    if (bounceHits[i].CompareTag("Box"))
                    {
                        bounceHits[i].GetComponent<Crate>().OnPlayerHeadBounce(pc); //Chocar con cabeza
                    }
                }
            }

        }
        



    }

    void DrawDebugSphere(Vector3 center, float radius, Color color, int segments = 20)
    {
        float angleStep = 360f / segments;

        // círculo en el plano XZ
        for (int i = 0; i < segments; i++)
        {
            float angleA = Mathf.Deg2Rad * (i * angleStep);
            float angleB = Mathf.Deg2Rad * ((i + 1) * angleStep);

            Vector3 pointA = center + new Vector3(Mathf.Cos(angleA), 0, Mathf.Sin(angleA)) * radius;
            Vector3 pointB = center + new Vector3(Mathf.Cos(angleB), 0, Mathf.Sin(angleB)) * radius;
            Debug.DrawLine(pointA, pointB, color);
        }

        // círculo en el plano XY
        for (int i = 0; i < segments; i++)
        {
            float angleA = Mathf.Deg2Rad * (i * angleStep);
            float angleB = Mathf.Deg2Rad * ((i + 1) * angleStep);

            Vector3 pointA = center + new Vector3(Mathf.Cos(angleA), Mathf.Sin(angleA), 0) * radius;
            Vector3 pointB = center + new Vector3(Mathf.Cos(angleB), Mathf.Sin(angleB), 0) * radius;
            Debug.DrawLine(pointA, pointB, color);
        }

        // círculo en el plano YZ
        for (int i = 0; i < segments; i++)
        {
            float angleA = Mathf.Deg2Rad * (i * angleStep);
            float angleB = Mathf.Deg2Rad * ((i + 1) * angleStep);

            Vector3 pointA = center + new Vector3(0, Mathf.Cos(angleA), Mathf.Sin(angleA)) * radius;
            Vector3 pointB = center + new Vector3(0, Mathf.Cos(angleB), Mathf.Sin(angleB)) * radius;
            Debug.DrawLine(pointA, pointB, color);
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
