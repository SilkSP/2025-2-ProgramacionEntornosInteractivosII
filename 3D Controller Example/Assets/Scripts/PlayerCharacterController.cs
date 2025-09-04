using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerCharacterController : MonoBehaviour
{
    public PlayerStats stats;


    [Header("Stats")]
    public float speed = 10f;
    private float gravity = -9.8f;
    public float gravityScale = 5f;
    private float defaultGravityScale;

    public float baseJumpForce = 30f;
    public float[] jumpForcesMultipliers; // [0] = SaltoA, [1] = SaltoB, [2] = SaltoC

    Vector3 velocity;
    Vector2 moveInput;
    Vector3 moveDirection;

    CharacterController characterController;

    [Header("Ataques")]
    public bool isAttacking = false;
    public float attackRadius = 5f;
    public float attackDuration = 0.4f;
    public float attackCooldownDuration = 0.5f;
    private bool attackOnCooldown = false;
    public GameObject spinEffectMesh;
    public float attackSpinSpeed = 720f;

    [Header("Saltos")]
    public float coyoteTime = 0.2f;
    private float coyoteTimeCounter;

    public float continuousJumpTimerDefault = 0.3f;
    private float continuousJumpTimer = 0f;
    public float minHorizontalSpeedJumpC = 0.1f;
    public float gravityScaleJumpC = 3f;

    public float jumpHorizontalMultiplier = 1f;
    public float jumpExtraDamping = 6f;

    private Vector3 jumpExtraHorizontal = Vector3.zero;

    private Vector3 lastEffectiveForward = Vector3.forward;   // dirección horizontal efectiva guardada
    private Vector3 smoothedGroundNormal = Vector3.up;        // para filtrar normals del terreno (no usado actualmente)
    public float groundNormalSmoothSpeed = 20f;               // tuning: 10-30 suele funcionar
    private Quaternion frozenRotation = Quaternion.identity;  // rotación congelada cuando no hay movimiento
    private bool isRotationFrozen = false;                    // flag si estamos congelando rotación
    public float minSpeedForRotation = 0.1f;                  // umbral para considerar movimiento
    public float yawSmoothingSpeed = 10f;                     // velocidad de interpolación de rotación

    // Hacer jump buffering
    private Vector3 jumpDirection;
    public float slopeUpBlend = 0.5f;

    public enum NextJumpEnum { JumpA = 0, JumpB = 1, JumpC = 2 }

    public NextJumpEnum nextJumpState = NextJumpEnum.JumpA;
    private NextJumpEnum pendingJumpState = NextJumpEnum.JumpA;

    private bool wasGroundedLastFrame = false;
    private bool allowJumpCancel = false;

    private bool isInJumpC = false;

    [Header("Pendientes")]
    private float raycastDistance;
    public float slopeRotationSpeed = 10f;
    private Vector3 lastGroundNormal = Vector3.up;

    // Umbral para considerar que había input al saltar
    public float moveInputThresholdForRotationChoice = 0.1f;

    // Decisión simple al saltar: si true, rotamos según movimiento; si false, según pendiente
    private bool rotateToMovementOnJump = true;

    [Header("Visuales")]
    public Transform characterMeshTransform;
    private float accumulatedJumpSpinDegrees = 0f;
    public float jumpSpinDegreesPerSecond = 36f;
    private bool wasInJumpCLastFrame = false;

    private Quaternion rotationWithoutSpinLocal = Quaternion.identity;
    private float accumulatedAttackSpinDegrees = 0f;
    private bool wasAttackingLastFrame = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        defaultGravityScale = gravityScale;

        jumpDirection = Vector3.up;

        if (jumpForcesMultipliers == null || jumpForcesMultipliers.Length < 3) // valores por defecto
        {
            jumpForcesMultipliers = new float[3];
            jumpForcesMultipliers[0] = 1f;
            jumpForcesMultipliers[1] = 1.3f;
            jumpForcesMultipliers[2] = 1.5f;
        }

        continuousJumpTimer = 0f;

        if (spinEffectMesh != null)
        {
            spinEffectMesh.SetActive(false);
        }

        if (characterMeshTransform != null)
        {
            rotationWithoutSpinLocal = characterMeshTransform.localRotation;
        }
    }

    void Update()
    {
        raycastDistance = characterController.height / 2 + 0.5f;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance, LayerMask.GetMask("Ground")))
        {
            lastGroundNormal = hit.normal;

            //smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, hit.normal, Time.deltaTime * groundNormalSmoothSpeed);
            //lastGroundNormal = smoothedGroundNormal;
        }
        else
        {
            lastGroundNormal = Vector3.up;

            //smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, Vector3.up, Time.deltaTime * groundNormalSmoothSpeed);
            //lastGroundNormal = smoothedGroundNormal;
        }

        if (characterController.isGrounded && velocity.y < 0)
        {
            if (lastGroundNormal == Vector3.up)
            {
                velocity.y = -2f;
            }
            else
            {
                // keep current behaviour for non-flat ground
            }
        }
        else
        {
            velocity.y += gravity * gravityScale * Time.deltaTime;
        }

        // evitar shadowing: asignar al campo moveDirection directamente
        moveDirection = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));
        moveDirection = Vector3.ProjectOnPlane(moveDirection, lastGroundNormal).normalized;

        Vector3 horizontalVelocity = new Vector3(this.moveDirection.x, 0f, this.moveDirection.z) * speed;
        Vector3 verticalVelocity = new Vector3(0, velocity.y + this.moveDirection.y, 0);

        jumpExtraHorizontal = Vector3.Lerp(jumpExtraHorizontal, Vector3.zero, Time.deltaTime * jumpExtraDamping);

        Vector3 newVelocity = horizontalVelocity + jumpExtraHorizontal + verticalVelocity;
        characterController.Move(newVelocity * Time.deltaTime);

        if (characterController.isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (transform.position.y < -50)
        {
            if(stats != null)
            {
                stats.vidas = stats.vidas - 1;
                stats.cajasDestruidas = 0;
                if(stats.vidas < 0)
                {
                    Application.Quit();
                }
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);





        }

        if (isAttacking)
        {
            Collider[] attackHits = Physics.OverlapSphere(transform.position, attackRadius);
            foreach (Collider attackHit in attackHits)
            {
                if (attackHit.CompareTag("Box"))
                {
                    DestroyCrateEvent(attackHit.GetComponent<Crate>());
                    
                }
            }
        }

        if (characterController.isGrounded)
        {
            if (!wasGroundedLastFrame)
            {
                OnLanded();
            }

            if (continuousJumpTimer > 0f)
            {
                continuousJumpTimer -= Time.deltaTime;
                if (continuousJumpTimer <= 0f)
                {
                    continuousJumpTimer = 0f;
                    nextJumpState = NextJumpEnum.JumpA;
                    pendingJumpState = NextJumpEnum.JumpA;
                }
            }
        }

        wasGroundedLastFrame = characterController.isGrounded;
    }

    private void LateUpdate()
    {
        if (characterMeshTransform == null) return;

        Vector3 inputHorizontal = moveDirection * speed;
        Vector3 effectiveHorizontal = inputHorizontal + jumpExtraHorizontal;
        float horizontalSpeed = effectiveHorizontal.magnitude;


        Vector3 upForLook = Vector3.up;
        if (characterController != null && characterController.isGrounded)
        {
            upForLook = lastGroundNormal;
        }

        
        Vector3 inputHorizDir = Vector3.zero;
        if (inputHorizontal.sqrMagnitude > 0.000001f)
        {
            inputHorizDir = inputHorizontal.normalized;
        }
        Vector3 inputHorizVec = new Vector3(inputHorizDir.x, 0f, inputHorizDir.z);

        if (characterController != null && characterController.isGrounded)
        {
            lastEffectiveForward = inputHorizVec.normalized;
        }
        else
        {
            // EN AIRE:
            if (rotateToMovementOnJump)
            {
                if (inputHorizVec.sqrMagnitude > 0.000001f)
                {
                    lastEffectiveForward = inputHorizVec.normalized;
                }
            }
            else
            {
                if (lastGroundNormal.sqrMagnitude > 0.000001f)
                {
                    Vector3 outward = lastGroundNormal.normalized;
                    outward.y = 0f;
                    if (outward.sqrMagnitude > 0.000001f)
                    {
                        lastEffectiveForward = outward.normalized;
                    }
                }
            }

            // Si el jugador empuja con fuerza mientras está en el aire, le devolvemos control
            bool strongInput = moveInput.sqrMagnitude > (moveInputThresholdForRotationChoice * moveInputThresholdForRotationChoice);
            if (strongInput)
            {
                rotateToMovementOnJump = true;
            }
        }

        Vector3 forwardOnSlope;
        if (!characterController.isGrounded && !rotateToMovementOnJump)
        {
            Vector3 slopeForward = Vector3.zero;
            if (lastGroundNormal.sqrMagnitude > 0.000001f)
            {
                slopeForward = Vector3.ProjectOnPlane(Vector3.up, lastGroundNormal);
            }

            if (slopeForward.sqrMagnitude < 0.000001f)
            {
                slopeForward = Vector3.ProjectOnPlane(lastEffectiveForward, upForLook);
                if (slopeForward.sqrMagnitude < 0.000001f)
                {
                    slopeForward = Vector3.ProjectOnPlane(characterMeshTransform.forward, upForLook);
                }
            }

            slopeForward.Normalize();
            forwardOnSlope = slopeForward;
        }
        else
        {
            Vector3 f = Vector3.ProjectOnPlane(lastEffectiveForward, upForLook);
            if (f.sqrMagnitude < 0.000001f)
            {
                f = Vector3.ProjectOnPlane(characterMeshTransform.forward, upForLook);
            }
            f.Normalize();
            forwardOnSlope = f;
        }

        if (horizontalSpeed > minSpeedForRotation || isInJumpC || (characterController.isGrounded && lastGroundNormal.sqrMagnitude > 0.000001f))
        {
            Quaternion targetRotation = Quaternion.LookRotation(forwardOnSlope, upForLook);
            characterMeshTransform.rotation = Quaternion.Slerp(characterMeshTransform.rotation, targetRotation, Time.deltaTime * yawSmoothingSpeed);

            frozenRotation = characterMeshTransform.rotation;
            isRotationFrozen = false;
            rotationWithoutSpinLocal = characterMeshTransform.localRotation;
        }
        else
        {
            if (!isRotationFrozen)
            {
                frozenRotation = characterMeshTransform.rotation;
                isRotationFrozen = true;
            }
            characterMeshTransform.rotation = frozenRotation;
            rotationWithoutSpinLocal = characterMeshTransform.localRotation;
        }

        if (isAttacking)
        {
            accumulatedAttackSpinDegrees += attackSpinSpeed * Time.deltaTime;
            accumulatedAttackSpinDegrees %= 360f;
            Quaternion attackSpinLocal = Quaternion.Euler(0f, accumulatedAttackSpinDegrees, 0f);
            characterMeshTransform.localRotation = rotationWithoutSpinLocal * attackSpinLocal;
            wasAttackingLastFrame = true;
        }
        else
        {
            if (wasAttackingLastFrame)
            {
                characterMeshTransform.localRotation = Quaternion.Slerp(characterMeshTransform.localRotation, rotationWithoutSpinLocal, Time.deltaTime * 10f);
                if (Quaternion.Angle(characterMeshTransform.localRotation, rotationWithoutSpinLocal) < 0.5f)
                {
                    characterMeshTransform.localRotation = rotationWithoutSpinLocal;
                    accumulatedAttackSpinDegrees = 0f;
                    wasAttackingLastFrame = false;
                }
            }
            else
            {
                if (isInJumpC)
                {
                    accumulatedJumpSpinDegrees += jumpSpinDegreesPerSecond * Time.deltaTime;
                    accumulatedJumpSpinDegrees %= 360f;
                    Quaternion jumpSpinLocal = Quaternion.Euler(accumulatedJumpSpinDegrees, 0f, 0f);
                    characterMeshTransform.localRotation = rotationWithoutSpinLocal * jumpSpinLocal;
                    wasInJumpCLastFrame = true;
                }
                else
                {
                    if (wasInJumpCLastFrame)
                    {
                        characterMeshTransform.localRotation = Quaternion.Slerp(characterMeshTransform.localRotation, rotationWithoutSpinLocal, Time.deltaTime * 10f);
                        if (Quaternion.Angle(characterMeshTransform.localRotation, rotationWithoutSpinLocal) < 0.5f)
                        {
                            characterMeshTransform.localRotation = rotationWithoutSpinLocal;
                            accumulatedJumpSpinDegrees = 0f;
                            wasInJumpCLastFrame = false;
                        }
                    }
                    else
                    {
                        characterMeshTransform.localRotation = rotationWithoutSpinLocal;
                    }
                }
            }
        }

    }
    private void OnLanded()
    {
        continuousJumpTimer = continuousJumpTimerDefault;
        nextJumpState = pendingJumpState;

        if (isInJumpC)
        {
            gravityScale = defaultGravityScale;
            isInJumpC = false;
        }

        pendingJumpState = nextJumpState;
        jumpDirection = Vector3.up;
        jumpExtraHorizontal = Vector3.zero;

        rotateToMovementOnJump = true;
    }

    private void ApplyJump(float multiplier)
    {
        Vector3 dir = jumpDirection.normalized;

        Vector3 impulse = dir * baseJumpForce * multiplier;

        velocity.y = impulse.y;

        Vector3 impulseHorizontal = new Vector3(impulse.x, 0f, impulse.z);
        jumpExtraHorizontal += impulseHorizontal * jumpHorizontalMultiplier;

    }

    private void ExecuteGroundJumpA()
    {
        ApplyJump(jumpForcesMultipliers[0]);
        allowJumpCancel = true;
        isInJumpC = false;
        gravityScale = defaultGravityScale;
        pendingJumpState = NextJumpEnum.JumpB;
    }

    private void ExecuteGroundJumpB()
    {
        ApplyJump(jumpForcesMultipliers[1]);
        allowJumpCancel = false;
        isInJumpC = false;
        gravityScale = defaultGravityScale;
        pendingJumpState = NextJumpEnum.JumpC;
    }

    private void ExecuteGroundJumpC()
    {
        ApplyJump(jumpForcesMultipliers[2]);
        allowJumpCancel = false;
        isInJumpC = true;
        gravityScale = gravityScaleJumpC;
        pendingJumpState = NextJumpEnum.JumpA;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void ComputeJumpDirectionBySlope()
    {
        if (lastGroundNormal == Vector3.zero)
        {
            jumpDirection = Vector3.up;
            return;
        }

        float t = Mathf.Clamp01(slopeUpBlend);

        if (lastGroundNormal != Vector3.up)
        {
            Vector3 blendedDirection = Vector3.Slerp(Vector3.up, lastGroundNormal.normalized, t);
            jumpDirection = blendedDirection.normalized;
        }
        else
        {
            jumpDirection = Vector3.up;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && coyoteTimeCounter > 0f)
        {
            ComputeJumpDirectionBySlope();

            if (continuousJumpTimer > 0f)
            {
                if (nextJumpState == NextJumpEnum.JumpA)
                {
                    ExecuteGroundJumpA();
                }
                else if (nextJumpState == NextJumpEnum.JumpB)
                {
                    ExecuteGroundJumpB();
                }
                else if (nextJumpState == NextJumpEnum.JumpC)
                {
                    Vector3 horizontalMovement = new Vector3(moveDirection.x, 0f, moveDirection.z);
                    if (horizontalMovement.magnitude > minHorizontalSpeedJumpC)
                    {
                        ExecuteGroundJumpC();
                    }
                    else
                    {
                        nextJumpState = NextJumpEnum.JumpA;
                        ExecuteGroundJumpA();
                    }
                }
            }
            else
            {
                nextJumpState = NextJumpEnum.JumpA;
                ExecuteGroundJumpA();
            }

            bool hadMovement = moveInput.sqrMagnitude > (moveInputThresholdForRotationChoice * moveInputThresholdForRotationChoice);
            rotateToMovementOnJump = hadMovement;

            if (!hadMovement)
            {
                Vector3 slopeForward = Vector3.zero;
                if (lastGroundNormal.sqrMagnitude > 0.000001f)
                {
                    slopeForward = Vector3.ProjectOnPlane(Vector3.up, lastGroundNormal);
                }

                if (slopeForward.sqrMagnitude > 0.000001f)
                {
                    Vector3 slopeHoriz = new Vector3(slopeForward.x, 0f, slopeForward.z);
                    if (slopeHoriz.sqrMagnitude > 0.000001f)
                    {
                        lastEffectiveForward = slopeHoriz.normalized;
                    }
                }
            }

            coyoteTimeCounter = 0f;
            wasGroundedLastFrame = false;
        }

        if (context.canceled)
        {
            coyoteTimeCounter = 0f;
            if (allowJumpCancel && velocity.y > 0f)
            {
                velocity.y *= 0.5f;
                allowJumpCancel = false;
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!isAttacking && !attackOnCooldown)
            {
                StartCoroutine(PerformAttack());
            }
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        accumulatedAttackSpinDegrees = 0f;
        if (spinEffectMesh != null)
        {
            spinEffectMesh.SetActive(true);
        }

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        if (spinEffectMesh != null)
        {
            spinEffectMesh.SetActive(false);
        }

        StartCoroutine(AttackCooldown());
    }

    private IEnumerator AttackCooldown()
    {
        attackOnCooldown = true;
        yield return new WaitForSeconds(attackCooldownDuration);
        attackOnCooldown = false;
    }

    public void DestroyCrateEvent(Crate crate)
    {
        crate.OnDestroyCrate();

        if (stats != null)
        {
            stats.cajasDestruidas = stats.cajasDestruidas + 1;
        }
        Destroy(crate.gameObject);

    }

    public void GrabFruitEvent()
    {
        if (stats != null)
        {
            stats.frutas = stats.frutas + 1;
        }
    }

    public void GrabGemEvent()
    {
        if (stats != null)
        {
            stats.gemas = stats.gemas + 1;
        }
    }
}


