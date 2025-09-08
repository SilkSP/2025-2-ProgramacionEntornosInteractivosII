using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerCharacterController : MonoBehaviour
{
    public PlayerStats stats;

    [Header("Stats")]
    public float speed = 10f;
    public float gravity = -9.8f;
    public float gravityScale = 5f;
    public float defaultGravityScale;

    public float baseJumpForce = 30f;
    public float[] jumpForcesMultipliers; // [0] = SaltoA, [1] = SaltoB, [2] = SaltoC

    public Vector3 velocity;
    public Vector2 moveInput;
    public Vector3 moveDirection;

    public CharacterController characterController;

    [Header("Ataques")]
    public bool isAttacking = false;
    public float attackRadius = 5f;
    public float attackDuration = 0.4f;
    public float attackCooldownDuration = 0.5f;
    public bool attackOnCooldown = false;
    public GameObject spinEffectMesh;
    public float attackSpinSpeed = 720f;

    [Header("Saltos")]
    public float coyoteTime = 0.2f;
    public float coyoteTimeCounter;

    public float continuousJumpTimerDefault = 0.3f;
    public float continuousJumpTimer = 0f;
    public float minHorizontalSpeedJumpC = 0.1f;
    public float gravityScaleJumpC = 3f;

    public float jumpHorizontalMultiplier = 1f;
    public float jumpExtraDamping = 6f;

    public Vector3 jumpExtraHorizontal = Vector3.zero;

    public Vector3 lastEffectiveForward = Vector3.forward;   // dirección horizontal efectiva guardada
    public float groundNormalSmoothSpeed = 20f;               // tuning: 10-30 suele funcionar
    public Quaternion frozenRotation = Quaternion.identity;  // rotación congelada cuando no hay movimiento
    public bool isRotationFrozen = false;                    // flag si estamos congelando rotación
    public float minSpeedForRotation = 0.1f;                  // umbral para considerar movimiento
    public float yawSmoothingSpeed = 10f;                     // velocidad de interpolación de rotación

    public Vector3 jumpDirection;
    public float slopeUpBlend = 0.5f;

    public enum NextJumpEnum { JumpA = 0, JumpB = 1, JumpC = 2 }

    public NextJumpEnum nextJumpState = NextJumpEnum.JumpA;
    public NextJumpEnum pendingJumpState = NextJumpEnum.JumpA;

    public bool wasGroundedLastFrame = false;
    public bool allowJumpCancel = false;

    public bool isInJumpC = false;

    [Header("Pendientes")]
    public float raycastDistance;
    public float slopeRotationSpeed = 10f;
    public Vector3 lastGroundNormal = Vector3.up;

    // Umbral para considerar que había input al saltar
    public float moveInputThresholdForRotationChoice = 0.1f;

    // Decisión simple al saltar: si true, rotamos según movimiento; si false, según pendiente
    public bool rotateToMovementOnJump = true;

    [Header("Visuales")]
    public Transform characterMeshTransform;
    public float accumulatedJumpSpinDegrees = 0f;
    public float jumpSpinDegreesPerSecond = 36f;
    public bool wasInJumpCLastFrame = false;

    public Quaternion rotationWithoutSpinLocal = Quaternion.identity;
    public float accumulatedAttackSpinDegrees = 0f;
    public bool wasAttackingLastFrame = false;

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

    public void DestroyCrateEvent(Crate crate)
    {
        if (crate == null) return;
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

    //Hacer GameManager para agarrar cosas.
}
