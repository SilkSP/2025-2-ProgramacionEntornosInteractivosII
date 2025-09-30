using UnityEngine;
using UnityEngine.AI;

public class WalkerMoveTo : MonoBehaviour
{
    NavMeshAgent agent;


    //public Transform target;

    private GameObject player;
    private Transform target;

    public float maxFollowDistance = 5;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameplayManager.instance != null)
        {
            player = GameplayManager.instance.GetPlayerGameObject();
            target = player.transform;

            if(Vector3.Distance(target.position, this.gameObject.transform.position) <= maxFollowDistance)
            {
                agent.destination = target.position;
            }
            
        }
        
    }
}
