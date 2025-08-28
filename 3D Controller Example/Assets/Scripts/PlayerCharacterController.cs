using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerCharacterController : MonoBehaviour
{
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

    public enum NextJumpEnum { JumpA = 0, JumpB = 1, JumpC = 2 }

    public NextJumpEnum nextJumpState = NextJumpEnum.JumpA;
    private NextJumpEnum pendingJumpState = NextJumpEnum.JumpA;

    private bool wasGroundedLastFrame = false;
    private bool allowJumpCancel = false;

    private bool isInJumpC = false;


    [Header("Pendientes")]
    public float raycastDistance = 10f;
    public float slopeRotationSpeed = 10f;

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
        moveDirection = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));
        Vector3 horizontalVelocity = moveDirection * speed;

        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * gravityScale * Time.deltaTime;
        }

        Vector3 newVelocity = horizontalVelocity + new Vector3(0, velocity.y, 0);
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (characterMeshTransform != null)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance))
            {
                Quaternion newRotation = Quaternion.FromToRotation(characterMeshTransform.up, hit.normal) * characterMeshTransform.rotation;
                characterMeshTransform.rotation = Quaternion.Lerp(characterMeshTransform.rotation, newRotation, Time.deltaTime * slopeRotationSpeed);
            }
        }

        if (isAttacking)
        {
            Collider[] attackHits = Physics.OverlapSphere(transform.position, attackRadius);
            foreach (Collider attackHit in attackHits)
            {
                if (attackHit.CompareTag("Box"))
                {
                    Destroy(attackHit.gameObject);
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
        if (moveDirection.sqrMagnitude > 0.01f && characterMeshTransform != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            characterMeshTransform.rotation = Quaternion.Lerp(characterMeshTransform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (characterMeshTransform != null)
        {
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
    }

    private void ExecuteGroundJumpA()
    {
        velocity.y = baseJumpForce * jumpForcesMultipliers[0];
        allowJumpCancel = true;
        isInJumpC = false;
        gravityScale = defaultGravityScale;
        pendingJumpState = NextJumpEnum.JumpB;
    }

    private void ExecuteGroundJumpB()
    {
        velocity.y = baseJumpForce * jumpForcesMultipliers[1];
        allowJumpCancel = false;
        isInJumpC = false;
        gravityScale = defaultGravityScale;
        pendingJumpState = NextJumpEnum.JumpC;
    }

    private void ExecuteGroundJumpC()
    {
        velocity.y = baseJumpForce * jumpForcesMultipliers[2];
        allowJumpCancel = false;
        isInJumpC = true;
        gravityScale = gravityScaleJumpC;
        pendingJumpState = NextJumpEnum.JumpA;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && coyoteTimeCounter > 0f)
        {
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

}
