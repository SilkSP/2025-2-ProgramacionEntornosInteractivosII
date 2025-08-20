using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerCharacterController : MonoBehaviour
{
    public float speed = 10f;
    private float gravity = -9.8f;
    public float gravityScale = 1f;

    public float jumpForce = 10f;

    Vector3 velocity;
    Vector2 moveInput;
    Vector3 moveDirection;

    CharacterController characterController;

    public float coyoteTime = 0.2f;
    private float coyoteTimeCounter;


    public Transform characterMeshTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        

    }

    // Update is called once per frame
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

        Debug.Log("IsGrounded?: " + characterController.isGrounded);



        if(characterController.isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }


        Debug.Log(coyoteTimeCounter);

        if(transform.position.y < -50)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

    }

    private void LateUpdate()
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            characterMeshTransform.rotation = Quaternion.Lerp(characterMeshTransform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        //if (context.performed && characterController.isGrounded)
        if(context.performed && coyoteTimeCounter > 0f)
        {
            Debug.Log("JUMP!");
            velocity.y = jumpForce;
        }

        if(context.canceled)
        {
            coyoteTimeCounter = 0f;
            if(velocity.y > 0f)
            {
                velocity.y *= 0.5f;
            }
        }


    }

}
