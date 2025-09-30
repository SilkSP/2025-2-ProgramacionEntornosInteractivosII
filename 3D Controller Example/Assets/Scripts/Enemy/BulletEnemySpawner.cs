using System.Collections;
using UnityEngine;

public class BulletEnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    public float time;

    public bool mustSpawnHomingBullets = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnEnemy());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator SpawnEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab);
        enemy.transform.SetPositionAndRotation(transform.position, transform.rotation);
        enemy.GetComponent<BulletEnemyMovement>().isAHomingBullet = mustSpawnHomingBullets;
        yield return new WaitForSeconds(5);

        StartCoroutine(SpawnEnemy());
    }
}
