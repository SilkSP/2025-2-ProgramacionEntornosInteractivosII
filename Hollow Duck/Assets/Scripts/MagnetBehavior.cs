using UnityEngine;

public class MagnetBehavior : MonoBehaviour
{
    public float detectionRadius = 3f;
    public float speed = 1.0f;

    bool isPlayerDetected = false;

    GameObject playerReference;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(playerReference != null)
        {
            if(isPlayerDetected)
            {
                float tempDistance = Vector3.Distance(transform.parent.position, playerReference.transform.position);
                if (tempDistance < detectionRadius)
                {
                    Debug.Log("MOVIENDO:" + transform.parent.position);

                    transform.parent.position = Vector3.MoveTowards(transform.parent.position, 
                        playerReference.transform.position, speed * Time.deltaTime);
                }
                
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            isPlayerDetected = true;
            playerReference = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerDetected = false;
            playerReference = null;
        }
    }
}
