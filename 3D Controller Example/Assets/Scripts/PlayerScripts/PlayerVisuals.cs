using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    PlayerCharacterController pc;

    void Start()
    {
        pc = GetComponent<PlayerCharacterController>();
        if (pc == null) Debug.LogWarning("PlayerVisuals: no se encontró PlayerCharacterController en el mismo GameObject.");
    }

    private void LateUpdate()
    {
        if (pc == null) return;
        if (pc.characterMeshTransform == null) return;

        Vector3 inputHorizontal = pc.moveDirection * pc.speed;
        Vector3 effectiveHorizontal = inputHorizontal + pc.jumpExtraHorizontal;
        float horizontalSpeed = effectiveHorizontal.magnitude;


        Vector3 upForLook = Vector3.up;
        if (pc.characterController != null && pc.characterController.isGrounded)
        {
            upForLook = pc.lastGroundNormal;
        }


        Vector3 inputHorizDir = Vector3.zero;
        if (inputHorizontal.sqrMagnitude > 0.000001f)
        {
            inputHorizDir = inputHorizontal.normalized;
        }
        Vector3 inputHorizVec = new Vector3(inputHorizDir.x, 0f, inputHorizDir.z);

        if (pc.characterController != null && pc.characterController.isGrounded)
        {
            pc.lastEffectiveForward = inputHorizVec.normalized;
        }
        else
        {
            // EN AIRE:
            if (pc.rotateToMovementOnJump)
            {
                if (inputHorizVec.sqrMagnitude > 0.000001f)
                {
                    pc.lastEffectiveForward = inputHorizVec.normalized;
                }
            }
            else
            {
                if (pc.lastGroundNormal.sqrMagnitude > 0.000001f)
                {
                    Vector3 outward = pc.lastGroundNormal.normalized;
                    outward.y = 0f;
                    if (outward.sqrMagnitude > 0.000001f)
                    {
                        pc.lastEffectiveForward = outward.normalized;
                    }
                }
            }

            // Si el jugador empuja con fuerza mientras está en el aire, le devolvemos control
            bool strongInput = pc.moveInput.sqrMagnitude > (pc.moveInputThresholdForRotationChoice * pc.moveInputThresholdForRotationChoice);
            if (strongInput)
            {
                pc.rotateToMovementOnJump = true;
            }
        }

        Vector3 forwardOnSlope;
        if (!pc.characterController.isGrounded && !pc.rotateToMovementOnJump)
        {
            Vector3 slopeForward = Vector3.zero;
            if (pc.lastGroundNormal.sqrMagnitude > 0.000001f)
            {
                slopeForward = Vector3.ProjectOnPlane(Vector3.up, pc.lastGroundNormal);
            }

            if (slopeForward.sqrMagnitude < 0.000001f)
            {
                slopeForward = Vector3.ProjectOnPlane(pc.lastEffectiveForward, upForLook);
                if (slopeForward.sqrMagnitude < 0.000001f)
                {
                    slopeForward = Vector3.ProjectOnPlane(pc.characterMeshTransform.forward, upForLook);
                }
            }

            slopeForward.Normalize();
            forwardOnSlope = slopeForward;
        }
        else
        {
            Vector3 f = Vector3.ProjectOnPlane(pc.lastEffectiveForward, upForLook);
            if (f.sqrMagnitude < 0.000001f)
            {
                f = Vector3.ProjectOnPlane(pc.characterMeshTransform.forward, upForLook);
            }
            f.Normalize();
            forwardOnSlope = f;
        }

        if (horizontalSpeed > pc.minSpeedForRotation || pc.isInJumpC || (pc.characterController.isGrounded && pc.lastGroundNormal.sqrMagnitude > 0.000001f))
        {
            Quaternion targetRotation = Quaternion.LookRotation(forwardOnSlope, upForLook);
            pc.characterMeshTransform.rotation = Quaternion.Slerp(pc.characterMeshTransform.rotation, targetRotation, Time.deltaTime * pc.yawSmoothingSpeed);

            pc.frozenRotation = pc.characterMeshTransform.rotation;
            pc.isRotationFrozen = false;
            pc.rotationWithoutSpinLocal = pc.characterMeshTransform.localRotation;
        }
        else
        {
            if (!pc.isRotationFrozen)
            {
                pc.frozenRotation = pc.characterMeshTransform.rotation;
                pc.isRotationFrozen = true;
            }
            pc.characterMeshTransform.rotation = pc.frozenRotation;
            pc.rotationWithoutSpinLocal = pc.characterMeshTransform.localRotation;
        }

        if (pc.isAttacking)
        {
            pc.accumulatedAttackSpinDegrees += pc.attackSpinSpeed * Time.deltaTime;
            pc.accumulatedAttackSpinDegrees %= 360f;
            Quaternion attackSpinLocal = Quaternion.Euler(0f, pc.accumulatedAttackSpinDegrees, 0f);
            pc.characterMeshTransform.localRotation = pc.rotationWithoutSpinLocal * attackSpinLocal;
            pc.wasAttackingLastFrame = true;
        }
        else
        {
            if (pc.wasAttackingLastFrame)
            {
                pc.characterMeshTransform.localRotation = Quaternion.Slerp(pc.characterMeshTransform.localRotation, pc.rotationWithoutSpinLocal, Time.deltaTime * 10f);
                if (Quaternion.Angle(pc.characterMeshTransform.localRotation, pc.rotationWithoutSpinLocal) < 0.5f)
                {
                    pc.characterMeshTransform.localRotation = pc.rotationWithoutSpinLocal;
                    pc.accumulatedAttackSpinDegrees = 0f;
                    pc.wasAttackingLastFrame = false;
                }
            }
            else
            {
                if (pc.isInJumpC)
                {
                    pc.accumulatedJumpSpinDegrees += pc.jumpSpinDegreesPerSecond * Time.deltaTime;
                    pc.accumulatedJumpSpinDegrees %= 360f;
                    Quaternion jumpSpinLocal = Quaternion.Euler(pc.accumulatedJumpSpinDegrees, 0f, 0f);
                    pc.characterMeshTransform.localRotation = pc.rotationWithoutSpinLocal * jumpSpinLocal;
                    pc.wasInJumpCLastFrame = true;
                }
                else
                {
                    if (pc.wasInJumpCLastFrame)
                    {
                        pc.characterMeshTransform.localRotation = Quaternion.Slerp(pc.characterMeshTransform.localRotation, pc.rotationWithoutSpinLocal, Time.deltaTime * 10f);
                        if (Quaternion.Angle(pc.characterMeshTransform.localRotation, pc.rotationWithoutSpinLocal) < 0.5f)
                        {
                            pc.characterMeshTransform.localRotation = pc.rotationWithoutSpinLocal;
                            pc.accumulatedJumpSpinDegrees = 0f;
                            pc.wasInJumpCLastFrame = false;
                        }
                    }
                    else
                    {
                        pc.characterMeshTransform.localRotation = pc.rotationWithoutSpinLocal;
                    }
                }
            }
        }
    }
}
