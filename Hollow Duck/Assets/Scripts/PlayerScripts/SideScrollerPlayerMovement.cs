using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SideScrollerPlayerMovement : MonoBehaviour
{
    SideScrollerPlayerCharacterController pc;

    [HideInInspector] public float defaultHeight;

    private Vector3 slideDirection = Vector3.zero;
    private float slideTimeRemaining = 0f;
    private bool slideContinueInAir = false;

    // Cooldown del slide
    public float slideCooldown = 1f; // ajustable en el inspector
    private bool slideOnCooldown = false;
    private float slideCooldownRemaining = 0f;

    void Awake()
    {
        pc = GetComponent<SideScrollerPlayerCharacterController>();
        if (pc == null) Debug.LogWarning("PlayerMovement: no se encontró PlayerCharacterController en el mismo GameObject.");
    }

    private void Start()
    {
        defaultHeight = pc.characterController.height;
        pc.isOnSlide = false;
        slideOnCooldown = false;
        slideCooldownRemaining = 0f;
    }

    void Update()
    {
        if (pc == null) return;


        if (transform.position.y < -50f)
        {
            pc.OnDamage();
        }

        pc.raycastDistance = pc.characterController.height / 2 + 0.5f;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, pc.raycastDistance, LayerMask.GetMask("Ground")))
        {
            pc.lastGroundNormal = hit.normal;
        }
        else
        {
            pc.lastGroundNormal = Vector3.up;
        }

        // --- GRAVEDAD: ahora se omite mientras el slide está activo (pc.isOnSlide && slideTimeRemaining > 0f)
        if (pc.characterController.isGrounded && pc.velocity.y < 0)
        {
            if (pc.lastGroundNormal == Vector3.up)
            {
                pc.velocity.y = -2f;
            }
            else
            {

            }
        }
        else
        {
            // No aplicar gravedad durante la ejecución del dash/slide activo
            if (!(pc.isOnSlide && slideTimeRemaining > 0f))
            {
                pc.velocity.y += pc.gravity * pc.gravityScale * Time.deltaTime;
            }
        }
        // --- fin cambio gravedad ---

        // NUEVO: mientras el slide esté activo y el personaje esté en el aire,
        // mantener la velocidad vertical en 0 hasta que termine el slide.
        if (pc.isOnSlide && !pc.characterController.isGrounded && slideTimeRemaining > 0f)
        {
            pc.velocity.y = 0f;
        }
        // --- fin nuevo ---

        if (!pc.isOnSlide)
        {
            pc.moveDirection = transform.TransformDirection(new Vector3(pc.moveInput.x, 0f, 0f));
            pc.moveDirection = Vector3.ProjectOnPlane(pc.moveDirection, pc.lastGroundNormal).normalized;
        }
        else
        {
            pc.moveDirection = Vector3.ProjectOnPlane(slideDirection, pc.lastGroundNormal).normalized;
        }

        float currentSpeed = pc.speed;
        // Se removió la lógica de crouch (no existe más), por lo tanto no aplicamos multipliers de crouch aquí.
        Vector3 horizontalVelocity = new Vector3(pc.moveDirection.x, 0f, pc.moveDirection.z) * currentSpeed;
        Vector3 verticalVelocity = new Vector3(0, pc.velocity.y + pc.moveDirection.y, 0);

        pc.jumpExtraHorizontal = Vector3.Lerp(pc.jumpExtraHorizontal, Vector3.zero, Time.deltaTime * pc.jumpExtraDamping);

        if (pc.isOnSlide && slideContinueInAir && pc.characterController.isGrounded && !pc.wasGroundedLastFrame)
        {
            pc.isOnSlide = false;
            slideTimeRemaining = 0f;
            slideContinueInAir = false;
            // no hay Uncrouch()
        }

        if (pc.isOnSlide)
        {
            if (!pc.characterController.isGrounded && !slideContinueInAir)
            {
                pc.isOnSlide = false;
                slideTimeRemaining = 0f;
                slideContinueInAir = false;
            }
            else
            {
                if (slideTimeRemaining > 0f)
                {
                    slideTimeRemaining -= Time.deltaTime;
                    horizontalVelocity = new Vector3(slideDirection.x, 0f, slideDirection.z) * (pc.speed * pc.slideSpeedMultiplier);
                }
                else
                {
                    bool isInAir = !pc.characterController.isGrounded;
                    if (isInAir && slideContinueInAir)
                    {
                        pc.isOnSlide = false;
                        slideTimeRemaining = 0f;
                        slideContinueInAir = false;

                        Vector3 slideVel = new Vector3(slideDirection.x, 0f, slideDirection.z) * (pc.speed * pc.slideSpeedMultiplier);
                        pc.jumpExtraHorizontal = slideVel;
                    }
                    else
                    {
                        pc.isOnSlide = false;
                        slideTimeRemaining = 0f;
                        slideContinueInAir = false;
                        // no hay Uncrouch()
                    }
                }
            }
        }

        // Decrementar cooldown si corresponde
        if (slideOnCooldown)
        {
            slideCooldownRemaining -= Time.deltaTime;
            if (slideCooldownRemaining <= 0f)
            {
                slideOnCooldown = false;
                slideCooldownRemaining = 0f;
            }
        }

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

                // Reactiva/reset del cooldown al tocar el suelo (se fuerza reinicio).
                ReactivateSlide();
            }

            if (pc.continuousJumpTimer > 0f)
            {
                pc.continuousJumpTimer -= Time.deltaTime;
                if (pc.continuousJumpTimer <= 0f)
                {
                    pc.continuousJumpTimer = 0f;
                    pc.nextJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpA;
                    pc.pendingJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpA;
                }
            }
        }

        pc.wasGroundedLastFrame = pc.characterController.isGrounded;



        Debug.DrawRay(transform.position, Vector3.up * (defaultHeight / 2f) * 1.1f, Color.green);

        // Nota: ya no usamos pc.isCrouchButtonPressed aquí para evitar re-disparos al salir al aire.
        // El Slide se inicia únicamente cuando OnCrouch recibe el evento performed.
    }

    /// <summary>
    /// Devuelve la dirección horizontal del facing del personaje basada en el signo de characterMeshTransform.localScale.y.
    /// Si no hay characterMeshTransform, usa transform.right por compatibilidad.
    /// (Usamos localScale.y porque solicitaste el signo en base a pc.characterMeshTransform.y)
    /// </summary>
    private Vector3 GetCharacterFacingDirection()
    {
        if (pc != null && pc.characterMeshTransform != null)
        {
            float signY = Mathf.Sign(pc.characterMeshTransform.localScale.y);
            return transform.right * signY;
        }
        else
        {
            return transform.right;
        }
    }

    public void Slide()
    {
        // Si está en cooldown, no se puede iniciar slide.
        if (slideOnCooldown) return;

        pc.isOnSlide = true;

        // Si iniciamos el slide en aire, permitir que continúe en aire.
        slideContinueInAir = !pc.characterController.isGrounded;

        Vector3 input = new Vector3(pc.moveInput.x, 0f, 0f);
        if (input.sqrMagnitude > 0.0001f)
        {
            slideDirection = transform.TransformDirection(input).normalized;
            slideDirection = Vector3.ProjectOnPlane(slideDirection, pc.lastGroundNormal).normalized;
        }
        else
        {
            float sign = (pc.characterMeshTransform.localRotation.eulerAngles.y - 180f);
            slideDirection = (sign > 0f) ? Vector3.back : Vector3.forward;
            //Debug.LogWarning(slideDirection + " / " + sign);
        }
        slideTimeRemaining = pc.slideDuration;

        // activar cooldown al usar el slide
        slideOnCooldown = true;
        slideCooldownRemaining = slideCooldown;
    }

    // Se eliminó Crouch(), Uncrouch() y CheckUp() porque la funcionalidad de agacharse se quitó.

    public void OnMove(InputAction.CallbackContext context)
    {
        if (pc == null) pc = GetComponent<SideScrollerPlayerCharacterController>();
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
        if (pc == null) pc = GetComponent<SideScrollerPlayerCharacterController>();

        if (context.performed && pc.coyoteTimeCounter > 0f)
        {
            ComputeJumpDirectionBySlope();

            // Si está en slide, ejecutar ExecuteSlideJump, si no, comportamiento normal.
            if (pc.isOnSlide)
            {
                ExecuteSlideJump();
            }

            else if (pc.continuousJumpTimer > 0f)
            {
                if (pc.nextJumpState == SideScrollerPlayerCharacterController.NextJumpEnum.JumpA)
                {
                    ExecuteGroundJumpA();
                }
                else if (pc.nextJumpState == SideScrollerPlayerCharacterController.NextJumpEnum.JumpB)
                {
                    ExecuteGroundJumpB();
                }
                else if (pc.nextJumpState == SideScrollerPlayerCharacterController.NextJumpEnum.JumpC)
                {
                    Vector3 horizontalMovement = new Vector3(pc.moveDirection.x, 0f, pc.moveDirection.z);
                    if (horizontalMovement.magnitude > pc.minHorizontalSpeedJumpC)
                    {
                        ExecuteGroundJumpC();
                    }
                    else
                    {
                        pc.nextJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpA;
                        ExecuteGroundJumpA();
                    }
                }
            }
            else
            {
                pc.nextJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpA;
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
        // Si estamos en slide activo y en el aire con tiempo restante, no aplicar componente vertical del salto.
        if (pc.isOnSlide && !pc.characterController.isGrounded && slideTimeRemaining > 0f)
        {
            Vector3 dir = pc.jumpDirection.normalized;

            Vector3 impulse = dir * pc.baseJumpForce * multiplier;

            // bloquear vertical
            pc.velocity.y = 0f;

            Vector3 impulseHorizontal = new Vector3(impulse.x, 0f, impulse.z);
            pc.jumpExtraHorizontal += impulseHorizontal * pc.jumpHorizontalMultiplier;

            return;
        }
        else
        {
            Vector3 dir = pc.jumpDirection.normalized;

            Vector3 impulse = dir * pc.baseJumpForce * multiplier;

            pc.velocity.y = impulse.y;

            Vector3 impulseHorizontal = new Vector3(impulse.x, 0f, impulse.z);
            pc.jumpExtraHorizontal += impulseHorizontal * pc.jumpHorizontalMultiplier;
        }
        
    }

    private void ExecuteGroundJumpA()
    {
        ApplyJump(pc.jumpForcesMultipliers[0]);
        pc.allowJumpCancel = true;
        pc.isInJumpC = false;
        pc.gravityScale = pc.defaultGravityScale;
        pc.pendingJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpB;
    }

    private void ExecuteGroundJumpB()
    {
        ApplyJump(pc.jumpForcesMultipliers[1]);
        pc.allowJumpCancel = false;
        pc.isInJumpC = false;
        pc.gravityScale = pc.defaultGravityScale;
        pc.pendingJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpC;
    }

    private void ExecuteGroundJumpC()
    {
        ApplyJump(pc.jumpForcesMultipliers[2]);
        pc.allowJumpCancel = false;
        pc.isInJumpC = true;
        pc.gravityScale = pc.gravityScaleJumpC;
        pc.pendingJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpA;
    }

    public void ExecuteBounceJump()
    {
        ApplyJump(pc.bounceJumpForceMultiplier);
        pc.allowJumpCancel = false;
        pc.isInJumpC = false;
        pc.gravityScale = pc.gravityScaleBounce;
        pc.pendingJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpA;
        ReactivateSlide();
    }

    public void ExecuteCrouchJump()
    {
        // Ya no hay crouch; si en algún momento lo necesitas, puedes adaptar este método o eliminarlo.
        ApplyJump(pc.crouchJumpForceMultiplier);
        pc.allowJumpCancel = false;
        pc.isInJumpC = false;
        pc.gravityScale = pc.gravityScaleJumpC;
        pc.pendingJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpA;
    }

    public void ExecuteSlideJump()
    {
        if (slideDirection.sqrMagnitude < 0.000001f)
        {
            Vector3 input = new Vector3(pc.moveInput.x, 0f, 0f);
            if (input.sqrMagnitude > 0.000001f)
            {
                slideDirection = transform.TransformDirection(input).normalized;
                slideDirection = Vector3.ProjectOnPlane(slideDirection, pc.lastGroundNormal).normalized;
            }
            else if (pc.lastEffectiveForward.sqrMagnitude > 0.000001f)
            {
                slideDirection = pc.lastEffectiveForward.normalized;
            }
            else
            {
                // Usar facing del personaje como fallback (signo de localScale.y)
                slideDirection = GetCharacterFacingDirection();
                slideDirection = Vector3.ProjectOnPlane(slideDirection, pc.lastGroundNormal).normalized;
            }
        }

        Vector3 slideVel = new Vector3(slideDirection.x, 0f, slideDirection.z) * (pc.speed * pc.slideSpeedMultiplier) * pc.slideJumpInertiaFactor;
        pc.jumpExtraHorizontal += slideVel;

        // MOVIDO: aplicar el salto mientras pc.isOnSlide aún está true para que ApplyJump pueda detectar el estado.
        ApplyJump(pc.jumpForcesMultipliers[0]);
        pc.allowJumpCancel = false;
        pc.isInJumpC = false;
        pc.gravityScale = pc.defaultGravityScale;
        pc.pendingJumpState = SideScrollerPlayerCharacterController.NextJumpEnum.JumpA;

        pc.isOnSlide = false;
        slideTimeRemaining = 0f;
        slideContinueInAir = false;

        // ya no existe Uncrouch()
    }


    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (pc == null) pc = GetComponent<SideScrollerPlayerCharacterController>();

        if (context.performed)
        {
            // Ahora el botón de "crouch" provoca directamente el Slide solo en el momento en que se aprieta.
            Slide();
        }
        // no hacemos nada en canceled (no usamos pc.isCrouchButtonPressed)
    }

    public void ForceStopVertical()
    {
        pc.velocity.y = 0f;
    }

    public void ReactivateSlide()
    {
        // Forzamos reinicio del cooldown al tocar el suelo.
        slideOnCooldown = false;
        slideCooldownRemaining = 0f;
    }

}
