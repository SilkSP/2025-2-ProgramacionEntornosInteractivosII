using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerCharacterController : MonoBehaviour
{
    public PlayerStats playerStats;

    [HideInInspector] public PlayerMovement pm;
    [HideInInspector] public PlayerAttacks pa;
    [HideInInspector] public PlayerVisuals pv;

    [Header("Stats")]
    public float speed = 10f;
    [HideInInspector] public float gravity = -9.8f;
    public float gravityScale = 5f;
    [HideInInspector] public float defaultGravityScale;

    public float baseJumpForce = 30f;
    public float[] jumpForcesMultipliers; // [0] = SaltoA, [1] = SaltoB, [2] = SaltoC

    public float bounceJumpForceMultiplier = 1f;
    public float crouchJumpForceMultiplier = 1.5f;


    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public Vector3 moveDirection;




    [Header("Slide")]
    [HideInInspector] public bool isCrouchButtonPressed;
    [HideInInspector] public bool isOnCrouch;
    [HideInInspector] public bool isOnSlide;

    public float slideDuration = 0.3f;               // duración del slide en segundos
    public float slideSpeedMultiplier = 2.5f;       // multiplicador de velocidad durante slide
    public float slideJumpInertiaFactor = 1.0f; // cuánto de la velocidad del slide se transfiere al salto (1 = 100%)
    public float crouchSpeedMultiplier = 0.25f; // velocidad cuando está en crouch y NO en slide


    [HideInInspector]
    public CharacterController characterController;

    [Header("Ataques")]
    [HideInInspector] public bool isAttacking = false;
    public float attackRadius = 5f;
    public float attackDuration = 0.4f;
    public float attackCooldownDuration = 0.5f;
    [HideInInspector] public bool attackOnCooldown = false;
    public GameObject spinEffectMesh;
    public float attackSpinSpeed = 720f;

    [Header("Saltos")]
    public float coyoteTime = 0.2f;
    [HideInInspector] public float coyoteTimeCounter;

    public float continuousJumpTimerDefault = 0.3f;
    [HideInInspector] public float continuousJumpTimer = 0f;
    public float minHorizontalSpeedJumpC = 0.1f;
    public float gravityScaleJumpC = 3f;

    public float jumpHorizontalMultiplier = 1f;
    public float jumpExtraDamping = 6f;

    [HideInInspector] public Vector3 jumpExtraHorizontal = Vector3.zero;

    [HideInInspector] public Vector3 lastEffectiveForward = Vector3.forward;    // dirección horizontal efectiva guardada
    public float groundNormalSmoothSpeed = 20f;                                 // tuning: 10-30 suele funcionar
    [HideInInspector] public Quaternion frozenRotation = Quaternion.identity;   // rotación congelada cuando no hay movimiento
    [HideInInspector] public bool isRotationFrozen = false;                     // flag si estamos congelando rotación
    public float minSpeedForRotation = 0.1f;                                    // umbral para considerar movimiento
    public float yawSmoothingSpeed = 10f;                                       // velocidad de interpolación de rotación

    [HideInInspector] public Vector3 jumpDirection;
    public float slopeUpBlend = 0.5f;

    public enum NextJumpEnum { JumpA = 0, JumpB = 1, JumpC = 2 }

    [HideInInspector] public NextJumpEnum nextJumpState = NextJumpEnum.JumpA;
    [HideInInspector] public NextJumpEnum pendingJumpState = NextJumpEnum.JumpA;

    [HideInInspector] public bool wasGroundedLastFrame = false;
    [HideInInspector] public bool allowJumpCancel = false;

    [HideInInspector] public bool isInJumpC = false;

    [Header("Pendientes")]
    public float raycastDistance;
    public float slopeRotationSpeed = 10f;
    [HideInInspector] public Vector3 lastGroundNormal = Vector3.up;

    // Umbral para considerar que había input al saltar
    public float moveInputThresholdForRotationChoice = 0.1f;

    // Decisión simple al saltar: si true, rotamos según movimiento; si false, según pendiente
    public bool rotateToMovementOnJump = true;

    [Header("Visuales")]
    public Transform characterMeshTransform;
    [HideInInspector] public float accumulatedJumpSpinDegrees = 0f;
    public float jumpSpinDegreesPerSecond = 36f;
    [HideInInspector] public bool wasInJumpCLastFrame = false;

    [HideInInspector] public Quaternion rotationWithoutSpinLocal = Quaternion.identity;
    [HideInInspector] public float accumulatedAttackSpinDegrees = 0f;
    [HideInInspector] public bool wasAttackingLastFrame = false;

    private void Awake()
    {
        if (pm == null) pm = GetComponent<PlayerMovement>();
        if (pa == null) pa = GetComponent<PlayerAttacks>();
        if (pv == null) pv = GetComponent<PlayerVisuals>();

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

    void Start()
    {
        
    }

    public void OnLanded()
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


    public void GrabFruitEvent()
    {
        if (playerStats != null)
        {
            playerStats.frutas = playerStats.frutas + 1;
        }
    }

    public void GrabGemEvent()
    {
        if (playerStats != null)
        {
            playerStats.gemas = playerStats.gemas + 1;
        }
    }

    public void OnDamage()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    //Hacer GameManager para agarrar cosas.
}
