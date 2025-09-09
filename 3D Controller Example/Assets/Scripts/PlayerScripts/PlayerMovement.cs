using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.Image;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    PlayerCharacterController pc;

    [HideInInspector] public float defaultHeight;


    void Awake()
    {
        pc = GetComponent<PlayerCharacterController>();
        if (pc == null) Debug.LogWarning("PlayerMovement: no se encontró PlayerCharacterController en el mismo GameObject.");
    }

    private void Start()
    {
        defaultHeight = pc.characterController.height;
    }

    void Update()
    {
        if (pc == null) return;

        pc.raycastDistance = pc.characterController.height / 2 + 0.5f;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, pc.raycastDistance, LayerMask.GetMask("Ground")))
        {
            pc.lastGroundNormal = hit.normal;
        }
        else
        {
            pc.lastGroundNormal = Vector3.up;
        }

        if (pc.characterController.isGrounded && pc.velocity.y < 0)
        {
            if (pc.lastGroundNormal == Vector3.up)
            {
                pc.velocity.y = -2f;
            }
            else
            {
                // keep current behaviour for non-flat ground
            }
        }
        else
        {
            pc.velocity.y += pc.gravity * pc.gravityScale * Time.deltaTime;
        }

        pc.moveDirection = transform.TransformDirection(new Vector3(pc.moveInput.x, 0f, pc.moveInput.y));
        pc.moveDirection = Vector3.ProjectOnPlane(pc.moveDirection, pc.lastGroundNormal).normalized;

        Vector3 horizontalVelocity = new Vector3(pc.moveDirection.x, 0f, pc.moveDirection.z) * pc.speed;
        Vector3 verticalVelocity = new Vector3(0, pc.velocity.y + pc.moveDirection.y, 0);

        pc.jumpExtraHorizontal = Vector3.Lerp(pc.jumpExtraHorizontal, Vector3.zero, Time.deltaTime * pc.jumpExtraDamping);

        Vector3 newVelocity = horizontalVelocity + pc.jumpExtraHorizontal + verticalVelocity;
        pc.characterController.Move(newVelocity * Time.deltaTime);

        if (pc.characterController.isGrounded)
        {
            pc.coyoteTimeCounter = pc.coyoteTime;
        }
        else
        {
            pc.coyoteTimeCounter -= Time.deltaTime;
        }

        if (pc.characterController.isGrounded)
        {
            if (!pc.wasGroundedLastFrame)
            {
                pc.OnLanded();
            }

            if (pc.continuousJumpTimer > 0f)
            {
                pc.continuousJumpTimer -= Time.deltaTime;
                if (pc.continuousJumpTimer <= 0f)
                {
                    pc.continuousJumpTimer = 0f;
                    pc.nextJumpState = PlayerCharacterController.NextJumpEnum.JumpA;
                    pc.pendingJumpState = PlayerCharacterController.NextJumpEnum.JumpA;
                }
            }
        }

        pc.wasGroundedLastFrame = pc.characterController.isGrounded;




        Debug.DrawRay(transform.position, Vector3.up * (defaultHeight / 2f) * 1.1f, Color.green);

        if (!pc.isOnCrouch && pc.characterController.isGrounded && pc.isCrouchButtonPressed)
        {
            Crouch();
        }
        else if(pc.isOnCrouch && !pc.isCrouchButtonPressed && !CheckUp())
        {
            Uncrouch();
        }

    }

    private bool CheckUp()
    {
        //Debug.LogWarning("ASDASDAJGIUOAFSIJUO");
        

        if (Physics.Raycast(transform.position + (Vector3.up * (defaultHeight / 4f)), Vector3.up, ((defaultHeight / 2f) * 1.05f)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Crouch()
    {
        pc.isOnCrouch = true;

        pc.characterController.height = defaultHeight / 2f;

        pc.characterController.Move(Vector3.down * (defaultHeight / 4f));

        pc.characterMeshTransform.localScale = new Vector3(
            pc.pv.defaultMeshScale.x, 
            pc.pv.defaultMeshScale.y / 2f, 
            pc.pv.defaultMeshScale.z);

    }
    public void Uncrouch()
    {
        pc.isOnCrouch = false;

        pc.characterController.height = defaultHeight;

        pc.characterController.Move(Vector3.up * (defaultHeight / 4f));

        pc.characterMeshTransform.localScale = pc.pv.defaultMeshScale;

    }


    public void OnMove(InputAction.CallbackContext context)
    {
        if (pc == null) pc = GetComponent<PlayerCharacterController>();
        pc.moveInput = context.ReadValue<Vector2>();
    }

    private void ComputeJumpDirectionBySlope()
    {
        if (pc.lastGroundNormal == Vector3.zero)
        {
            pc.jumpDirection = Vector3.up;
            return;
        }

        float t = Mathf.Clamp01(pc.slopeUpBlend);

        if (pc.lastGroundNormal != Vector3.up)
        {
            Vector3 blendedDirection = Vector3.Slerp(Vector3.up, pc.lastGroundNormal.normalized, t);
            pc.jumpDirection = blendedDirection.normalized;
        }
        else
        {
            pc.jumpDirection = Vector3.up;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (pc == null) pc = GetComponent<PlayerCharacterController>();

        if (context.performed && pc.coyoteTimeCounter > 0f)
        {
            ComputeJumpDirectionBySlope();

            if(pc.isOnCrouch)
            {
                ExecuteCrouchJump(); 
            }

            else if (pc.continuousJumpTimer > 0f)
            {
                if (pc.nextJumpState == PlayerCharacterController.NextJumpEnum.JumpA)
                {
                    ExecuteGroundJumpA();
                }
                else if (pc.nextJumpState == PlayerCharacterController.NextJumpEnum.JumpB)
                {
                    ExecuteGroundJumpB();
                }
                else if (pc.nextJumpState == PlayerCharacterController.NextJumpEnum.JumpC)
                {
                    Vector3 horizontalMovement = new Vector3(pc.moveDirection.x, 0f, pc.moveDirection.z);
                    if (horizontalMovement.magnitude > pc.minHorizontalSpeedJumpC)
                    {
                        ExecuteGroundJumpC();
                    }
                    else
                    {
                        pc.nextJumpState = PlayerCharacterController.NextJumpEnum.JumpA;
                        ExecuteGroundJumpA();
                    }
                }
            }
            else
            {
                pc.nextJumpState = PlayerCharacterController.NextJumpEnum.JumpA;
                ExecuteGroundJumpA();
            }

            bool hadMovement = pc.moveInput.sqrMagnitude > (pc.moveInputThresholdForRotationChoice * pc.moveInputThresholdForRotationChoice);
            pc.rotateToMovementOnJump = hadMovement;

            if (!hadMovement)
            {
                Vector3 slopeForward = Vector3.zero;
                if (pc.lastGroundNormal.sqrMagnitude > 0.000001f)
                {
                    slopeForward = Vector3.ProjectOnPlane(Vector3.up, pc.lastGroundNormal);
                }

                if (slopeForward.sqrMagnitude > 0.000001f)
                {
                    Vector3 slopeHoriz = new Vector3(slopeForward.x, 0f, slopeForward.z);
                    if (slopeHoriz.sqrMagnitude > 0.000001f)
                    {
                        pc.lastEffectiveForward = slopeHoriz.normalized;
                    }
                }
            }

            pc.coyoteTimeCounter = 0f;
            pc.wasGroundedLastFrame = false;
        }

        if (context.canceled)
        {
            pc.coyoteTimeCounter = 0f;
            CancelJump();
        }
    }

    public void CancelJump()
    {
        if (pc.allowJumpCancel && pc.velocity.y > 0f)
        {
            pc.velocity.y *= 0.5f;
            pc.allowJumpCancel = false;
        }
    }

    private void ApplyJump(float multiplier)
    {
        Vector3 dir = pc.jumpDirection.normalized;

        Vector3 impulse = dir * pc.baseJumpForce * multiplier;

        pc.velocity.y = impulse.y;

        Vector3 impulseHorizontal = new Vector3(impulse.x, 0f, impulse.z);
        pc.jumpExtraHorizontal += impulseHorizontal * pc.jumpHorizontalMultiplier;
    }

    private void ExecuteGroundJumpA()
    {
        ApplyJump(pc.jumpForcesMultipliers[0]);
        pc.allowJumpCancel = true;
        pc.isInJumpC = false;
        pc.gravityScale = pc.defaultGravityScale;
        pc.pendingJumpState = PlayerCharacterController.NextJumpEnum.JumpB;
    }

    private void ExecuteGroundJumpB()
    {
        ApplyJump(pc.jumpForcesMultipliers[1]);
        pc.allowJumpCancel = false;
        pc.isInJumpC = false;
        pc.gravityScale = pc.defaultGravityScale;
        pc.pendingJumpState = PlayerCharacterController.NextJumpEnum.JumpC;
    }

    private void ExecuteGroundJumpC()
    {
        ApplyJump(pc.jumpForcesMultipliers[2]);
        pc.allowJumpCancel = false;
        pc.isInJumpC = true;
        pc.gravityScale = pc.gravityScaleJumpC;
        pc.pendingJumpState = PlayerCharacterController.NextJumpEnum.JumpA;
    }

    public void ExecuteBounceJump()
    {
        ApplyJump(pc.bounceJumpForceMultiplier);
        pc.allowJumpCancel = false;
        pc.isInJumpC = false;
        pc.gravityScale = pc.defaultGravityScale;
        pc.pendingJumpState = PlayerCharacterController.NextJumpEnum.JumpA;
    }

    public void ExecuteCrouchJump()
    {
        Uncrouch();
        ApplyJump(pc.crouchJumpForceMultiplier);
        pc.allowJumpCancel = false;
        pc.isInJumpC = false;
        pc.gravityScale = pc.gravityScaleJumpC;
        pc.pendingJumpState = PlayerCharacterController.NextJumpEnum.JumpA;
    }




    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (pc == null) pc = GetComponent<PlayerCharacterController>();

        if (context.performed)
        {
            pc.isCrouchButtonPressed = true;
        }
        if (context.canceled)
        {
            pc.isCrouchButtonPressed = false;
        }
    }


    public void ForceStopVertical()
    {
        pc.velocity.y = 0f;
    }

}
