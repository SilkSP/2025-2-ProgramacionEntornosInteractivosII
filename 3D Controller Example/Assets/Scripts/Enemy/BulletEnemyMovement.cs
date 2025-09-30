using System.Collections;
using UnityEngine;

public class BulletEnemyMovement : MonoBehaviour
{
    Rigidbody rb;

    Vector3 direction;

    public float explodeTime = 3.5f;

    public float speed = 0.15f;


    public GameObject homingTarget;

    public bool isAHomingBullet = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        direction = transform.forward;

        StartCoroutine(Explode());


        homingTarget = GameObject.FindWithTag("Player");

        if( homingTarget == null )
        {
            isAHomingBullet = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if( isAHomingBullet )
        {
            direction = (homingTarget.transform.position - transform.position).normalized;
            //direction = new Vector3(direction.x, direction.y, direction.z);
        }
        
        rb.AddForce(direction * speed, ForceMode.VelocityChange);

        
    }

    IEnumerator Explode()
    {

        yield return new WaitForSeconds(explodeTime);

        Destroy(this.gameObject);
    }

}
