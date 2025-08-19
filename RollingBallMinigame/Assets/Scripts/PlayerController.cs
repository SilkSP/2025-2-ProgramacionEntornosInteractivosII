using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    bool isJumpUnlocked = false;

    bool canJump;
    bool isJumping;

    bool canBounce;
    bool isBouncing;

    float bouncingLimit = 2;

    Rigidbody rb;

    Vector3 movementDirection = new Vector3(0, 0, 0);
    public float speed = 3;

    public float maxSpeed = 15;

    public float dashForce = 300;
    bool canUseDash = false;
    bool currentlyUsingDash = false;
    public float jumpForce = 30;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        canUseDash = true;
        currentlyUsingDash = false;
        canJump = true;
        isJumping = false;
        canBounce = false;
        isBouncing = false;
    }

    void OnMove(InputValue movementValue)
    {
        movementDirection = movementValue.Get<Vector2>();
        movementDirection = new Vector3(movementValue.Get<Vector2>().x, 0f, movementValue.Get<Vector2>().y);
    }

    private void Update()
    {
        if(transform.position.y < -100)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Keyboard.current.xKey.wasPressedThisFrame && canUseDash)
        {
            StartCoroutine(Dash(1f));
        }

        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if(canJump && !isJumping && !isBouncing && isJumpUnlocked)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isJumping = true;
                canBounce = true;
            }
            else if(isJumping && canBounce)
            {
                rb.AddForce(Vector3.down * jumpForce * 5, ForceMode.Impulse);
                isBouncing = true;
                isJumping = false;
                canBounce = false;
            }

        }


        //if(Keyboard.current.spaceKey.
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("On Collision Enter");
        if(collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log(collision.relativeVelocity);
                
                
            Vector3 normal = collision.GetContact(0).normal;
                
            Vector3 flattenedNormal = Vector3.ProjectOnPlane(normal, Vector3.up);

            rb.AddForce(flattenedNormal * collision.relativeVelocity.magnitude, ForceMode.VelocityChange);
        }
       
        if(collision.gameObject.CompareTag("Floor"))
        {
            if (isBouncing)
            {
                if (Mathf.Abs(collision.relativeVelocity.y) < bouncingLimit)
                {
                    isBouncing = false;
                    Debug.Log(Mathf.Abs(rb.linearVelocity.y));
                }
                else
                {
                    Vector3 normal = collision.GetContact(0).normal;
                    rb.AddForce(new Vector3(0, normal.y / 2, 0) * collision.relativeVelocity.magnitude, ForceMode.VelocityChange);
                }

            }
            else if(!isBouncing)
            {
                isJumping = false;
                canJump = true;
            }
        }
    }

    IEnumerator Dash(float duration)
    {
        Debug.Log("Comienza DASH");
        canUseDash = false;
        currentlyUsingDash = true;

        Vector3 direction = Vector3.ProjectOnPlane(rb.linearVelocity.normalized, Vector3.up);

        rb.linearVelocity = new Vector3(rb.linearVelocity.normalized.x, rb.linearVelocity.y, rb.linearVelocity.normalized.z);
        //rb.linearVelocity = rb.linearVelocity.normalized;
        rb.AddForce(direction * dashForce , ForceMode.Impulse);
        
        yield return new WaitForSeconds(duration);

        Debug.Log("Termina DASH");
        currentlyUsingDash = false;

        yield return new WaitForSeconds(1f);

        canUseDash = true;
    }


    private void OnTriggerEnter(Collider other)
    {
        /*if(other.gameObject.CompareTag("Collectable"))
        {
            Debug.Log("Collectable obtained!");
        }
        else if(other.gameObject.CompareTag("Finish"))
        {
            Application.Quit();
        }
        */

        switch(other.gameObject.tag)
        {
            case "Collectable":

                Debug.Log("Collectable obtained!");
                Destroy(other.gameObject);
                break;
            case "PowerUp":
                if(other.gameObject.GetComponent<PowerUp>().index == 1)
                {
                    isJumpUnlocked = true;
                    Destroy(other.gameObject);
                }
                else if (other.gameObject.GetComponent<PowerUp>().index == 2)
                {
                    //isDashUnlocked = true;
                    Destroy(other.gameObject);
                }
                break;
            case "Finish":
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }

    }


    private void FixedUpdate()
    {
        Vector3 force = movementDirection * speed;
        rb.AddForce(force);

        if(rb.linearVelocity.magnitude > maxSpeed && !currentlyUsingDash)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        //Debug.Log("Vertical Speed: " + rb.linearVelocity.y);

    }
}
