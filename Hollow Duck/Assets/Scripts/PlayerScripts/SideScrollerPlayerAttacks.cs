using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SideScrollerPlayerAttacks : MonoBehaviour
{
    SideScrollerPlayerCharacterController pc;
    private bool attackOnCooldownLocal = false;

    private Collider[] attackHits = new Collider[20]; // más grande por seguridad

    // Offsets / tuning
    public float attackForwardOffset = 0.5f;
    public float attackVerticalOffset = 0.2f;
    public float inputThreshold = 0.15f;

    // Suscripción directa
    PlayerInput playerInput;
    InputAction attackAction;

    // Estado del ataque actual (fijado al momento de iniciar el ataque)
    private Vector3 currentAttackPosition = Vector3.zero;
    private Vector3 currentAttackDir = Vector3.right;

    void Awake()
    {
        pc = GetComponent<SideScrollerPlayerCharacterController>();
        if (pc == null) Debug.LogWarning("PlayerAttacks: no se encontró PlayerCharacterController en el mismo GameObject.");

        // Intento auto-subscripción al InputAction "Attack"
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            attackAction = playerInput.actions.FindAction("Attack", true);
            if (attackAction != null)
            {
                attackAction.started += OnAttack;
                attackAction.performed += OnAttack;
                attackAction.canceled += OnAttack;
                Debug.Log("PlayerAttacks: subscripto a Attack action (started/performed/canceled).");
            }
            else
            {
                Debug.LogWarning("PlayerAttacks: no se encontró la Action 'Attack' en playerInput.actions");
            }
        }
        else
        {
            Debug.LogWarning("PlayerAttacks: no existe PlayerInput o actions en este GameObject");
        }
    }

    void OnDestroy()
    {
        if (attackAction != null)
        {
            attackAction.started -= OnAttack;
            attackAction.performed -= OnAttack;
            attackAction.canceled -= OnAttack;
        }
    }

    void Update()
    {
        if (pc == null) return;

        if (pc.isAttacking)
        {
            // Usamos la posición fija calculada al iniciar el ataque
            Vector3 characterAttackPosition = currentAttackPosition;

            // Si por alguna razón no fue inicializada, fallback a comportamiento por input actual
            if (characterAttackPosition == Vector3.zero)
            {
                Vector2 input = pc.moveInput;
                Vector3 attackDir = Vector3.zero;

                if (input.sqrMagnitude > inputThreshold * inputThreshold)
                {
                    if (Mathf.Abs(input.y) >= Mathf.Abs(input.x))
                    {
                        attackDir = (input.y > 0f) ? Vector3.up : Vector3.down;
                    }
                    else
                    {
                        // cuando el vertical no domina, usamos la dirección donde mira el personaje (basado en characterMeshTransform Y si existe)
                        bool facingRight = true;
                        if (pc != null && pc.characterMeshTransform != null)
                        {
                            float yRot = pc.characterMeshTransform.eulerAngles.y;
                            facingRight = Mathf.Abs(Mathf.DeltaAngle(yRot, 0f)) < 90f;
                        }
                        else
                        {
                            facingRight = transform.localScale.x >= 0f;
                        }
                        attackDir = facingRight ? transform.right : -transform.right;
                    }
                }
                else
                {
                    bool facingRight = true;
                    if (pc != null && pc.characterMeshTransform != null)
                    {
                        float yRot = pc.characterMeshTransform.eulerAngles.y;
                        facingRight = Mathf.Abs(Mathf.DeltaAngle(yRot, 0f)) < 90f;
                    }
                    else
                    {
                        facingRight = transform.localScale.x >= 0f;
                    }
                    attackDir = facingRight ? transform.right : -transform.right;
                }

                Vector3 baseCenter = transform.position + (pc.characterController != null ? pc.characterController.center : Vector3.zero);
                characterAttackPosition = baseCenter + attackDir.normalized * attackForwardOffset;
                if (attackDir == Vector3.up) characterAttackPosition += Vector3.up * attackVerticalOffset;
                if (attackDir == Vector3.down) characterAttackPosition += Vector3.down * attackVerticalOffset;
            }


            // Determinamos el centro de la comprobación en runtime: preferimos la posición del mesh si está activo
            Vector3 overlapCenter = characterAttackPosition;
            if (pc.attackColliderPosition != null && pc.attackColliderPosition.activeSelf)
            {
                //overlapCenter = pc.attackEffectMesh.transform.position;
                overlapCenter = pc.attackColliderPosition.transform.position;

            }
            //else if (pc.attackEffectSpawn != null)
            //{
            //   overlapCenter = pc.attackEffectSpawn.transform.position;
            //   overlapCenter = pc.attackColliderPosition.transform.position;
            //}




            DrawDebugSphere(overlapCenter, pc.attackRadius, Color.red);

            // Ejecutamos OverlapSphere y procesamos hits
            int attackHitsSize = Physics.OverlapSphereNonAlloc(overlapCenter, pc.attackRadius, attackHits);

            for (int i = 0; i < attackHitsSize; i++)
            {
                if (attackHits[i] != null)
                {
                    if (attackHits[i].CompareTag("Box"))
                    {
                        var crate = attackHits[i].GetComponent<Crate>();
                        if (crate != null)
                        {
                            crate.OnPlayerNeedleAttack(pc);
                            if (currentAttackDir.y < -0.5f)
                            {
                                pc.pm.ExecuteBounceJump();
                            }
                        }
                    }
                    else if (attackHits[i].CompareTag("Ground"))
                    {
                        if (currentAttackDir.y < -0.5f)
                            pc.pm.ExecuteBounceJump();
                    }
                }
            }

            if (pc.attackEffectMesh != null && pc.attackEffectMesh.activeSelf)
            {
                if (pc.attackEffectSpawn != null)
                {
                    pc.attackEffectMesh.transform.position = pc.attackEffectSpawn.transform.position;
                }
            }
        }
    }

    void DrawDebugSphere(Vector3 center, float radius, Color color, int segments = 20)
    {
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angleA = Mathf.Deg2Rad * (i * angleStep);
            float angleB = Mathf.Deg2Rad * ((i + 1) * angleStep);
            Vector3 pointA = center + new Vector3(Mathf.Cos(angleA), 0, Mathf.Sin(angleA)) * radius;
            Vector3 pointB = center + new Vector3(Mathf.Cos(angleB), 0, Mathf.Sin(angleB)) * radius;
            Debug.DrawLine(pointA, pointB, color);
        }
        for (int i = 0; i < segments; i++)
        {
            float angleA = Mathf.Deg2Rad * (i * angleStep);
            float angleB = Mathf.Deg2Rad * ((i + 1) * angleStep);
            Vector3 pointA = center + new Vector3(Mathf.Cos(angleA), Mathf.Sin(angleA), 0) * radius;
            Vector3 pointB = center + new Vector3(Mathf.Cos(angleB), Mathf.Sin(angleB), 0) * radius;
            Debug.DrawLine(pointA, pointB, color);
        }
        for (int i = 0; i < segments; i++)
        {
            float angleA = Mathf.Deg2Rad * (i * angleStep);
            float angleB = Mathf.Deg2Rad * ((i + 1) * angleStep);
            Vector3 pointA = center + new Vector3(0, Mathf.Cos(angleA), Mathf.Sin(angleA)) * radius;
            Vector3 pointB = center + new Vector3(0, Mathf.Cos(angleB), Mathf.Sin(angleB)) * radius;
            Debug.DrawLine(pointA, pointB, color);
        }
    }

    // Se reutiliza OnAttack como handler; ahora puede ser llamado por started/performed/canceled
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (pc == null) pc = GetComponent<SideScrollerPlayerCharacterController>();

        // DEBUG: ver fase y moveInput (así comprobás si falla por input)
        if (pc != null)
        {
            Debug.Log($"OnAttack phase:{context.phase} isAttacking:{pc.isAttacking} cooldown:{attackOnCooldownLocal} moveInput:{pc.moveInput}");
        }

        // Aceptamos tanto started como performed para asegurar que el ataque se dispare en distintas configs
        if ((context.started || context.performed) && !pc.isAttacking && !attackOnCooldownLocal)
        {
            // --- calculo y fijo la dirección y posición del ataque en el momento de presionar ---
            Vector2 input = pc.moveInput;
            Vector3 attackDir = Vector3.zero;

            if (input.sqrMagnitude > inputThreshold * inputThreshold)
            {
                if (Mathf.Abs(input.y) >= Mathf.Abs(input.x))
                {
                    attackDir = (input.y > 0f) ? Vector3.up : Vector3.down;
                }
                else
                {
                    // cuando el vertical no domina, usamos la dirección donde mira el personaje (basado en characterMeshTransform Y si existe)
                    bool facingRight = true;
                    if (pc != null && pc.characterMeshTransform != null)
                    {
                        float yRot = pc.characterMeshTransform.eulerAngles.y;
                        facingRight = Mathf.Abs(Mathf.DeltaAngle(yRot, 0f)) < 90f;
                    }
                    else
                    {
                        facingRight = transform.localScale.x >= 0f;
                    }
                    attackDir = facingRight ? transform.right : -transform.right;
                }
            }
            else
            {
                // sin input: adelante según hacia donde mira el personaje
                bool facingRight = true;
                if (pc != null && pc.characterMeshTransform != null)
                {
                    float yRot = pc.characterMeshTransform.eulerAngles.y;
                    facingRight = Mathf.Abs(Mathf.DeltaAngle(yRot, 0f)) < 90f;
                }
                else
                {
                    facingRight = transform.localScale.x >= 0f;
                }
                attackDir = facingRight ? transform.right : -transform.right;
            }

            // Si el jugador quiere atacar hacia abajo pero está en el suelo, lo convertimos a ataque lateral
            if (attackDir == Vector3.down && pc.characterController != null && pc.characterController.isGrounded)
            {
                bool facingRight = true;
                if (pc != null && pc.characterMeshTransform != null)
                {
                    float yRot = pc.characterMeshTransform.eulerAngles.y;
                    facingRight = Mathf.Abs(Mathf.DeltaAngle(yRot, 0f)) < 90f;
                }
                else
                {
                    facingRight = transform.localScale.x >= 0f;
                }
                attackDir = facingRight ? transform.right : -transform.right;
            }

            Vector3 baseCenter = transform.position + (pc.characterController != null ? pc.characterController.center : Vector3.zero);
            Vector3 characterAttackPosition = baseCenter + attackDir.normalized * attackForwardOffset;
            if (attackDir == Vector3.up) characterAttackPosition += Vector3.up * attackVerticalOffset;
            if (attackDir == Vector3.down) characterAttackPosition += Vector3.down * attackVerticalOffset;

            // almaceno la posición fija que usará Update para la detección
            currentAttackDir = attackDir.normalized;
            currentAttackPosition = characterAttackPosition;

            // --- CAMBIO: attackEffectMesh sigue al spawn y rota en X según currentAttackDir ---
            float xAngle = Mathf.Atan2(currentAttackDir.y, currentAttackDir.x) * Mathf.Rad2Deg;

            if (pc.attackEffectMesh != null)
            {
                if (pc.attackEffectSpawn != null)
                {
                    pc.attackEffectMesh.transform.position = pc.attackEffectSpawn.transform.position;
                }
                else
                {
                    pc.attackEffectMesh.transform.position = currentAttackPosition;
                }

                // rotamos el mesh en X según la dirección fijada
                pc.attackEffectMesh.transform.rotation = Quaternion.Euler(xAngle, 0f, 0f);

                pc.accumulatedAttackSpinDegrees = 0f;
                pc.attackEffectMesh.SetActive(true);
            }

            // marcamos isAttacking aquí para que Update dibuje la esfera inmediatamente
            pc.isAttacking = true;

            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        if (pc == null) yield break;

        Debug.Log("PerformAttack started");
        pc.isAttacking = true;
        pc.accumulatedAttackSpinDegrees = 0f;
        if (pc.attackEffectMesh != null)
        {
            pc.attackEffectMesh.SetActive(true);
        }

        yield return new WaitForSeconds(pc.attackDuration);

        pc.isAttacking = false;

        // desactivar y limpiar
        if (pc.attackEffectMesh != null)
        {
            pc.attackEffectMesh.SetActive(false);
        }

        // limpiamos la posición fija para evitar que quede colisionando por accidente
        currentAttackPosition = Vector3.zero;
        currentAttackDir = Vector3.right;

        StartCoroutine(AttackCooldown());
    }

    private IEnumerator AttackCooldown()
    {
        attackOnCooldownLocal = true;
        yield return new WaitForSeconds(pc.attackCooldownDuration);
        attackOnCooldownLocal = false;
    }
}
