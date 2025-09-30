using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;


public class PatrolMovement : MonoBehaviour
{

    public Transform[] waypoints;
    public int destinationIndex = 0;
    NavMeshAgent agent;

    float distanceToNextPoint;

    float distanceLimit = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        destinationIndex = 0;
        agent = GetComponent<NavMeshAgent>();
        if(waypoints.Length > 0 )
        {
            agent.destination = waypoints[0].position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        distanceToNextPoint = Vector3.Distance(transform.position, waypoints[destinationIndex].position);

        Debug.Log(distanceToNextPoint);
        if(distanceToNextPoint < distanceLimit)
        {
            ChangeIndex();
            agent.velocity = agent.velocity / 2f;

        }
    }

    public void ChangeIndex()
    {
        destinationIndex++;
        if(destinationIndex >= waypoints.Length)
        {
            destinationIndex = 0;
        }

        agent.destination = waypoints[destinationIndex].position;


    }

}
