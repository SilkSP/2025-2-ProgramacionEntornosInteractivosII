using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class LevelManager : MonoBehaviour
{
    [Header("Referencias")]
    public List<GameObject> roomPrefabs;
    public GameObject roomFloorPrefab;
    public GameObject roomWallPrefab;
    public GameObject roomExitPrefab;
    public GameObject playerReference;

    [Header("Enemigos")]
    public List<GameObject> enemyPrefabs;
    [Range(0f, 1f)] public float enemyProbability = 0.2f;
    public int maxEnemiesPerRoom = 1;

    [Header("Parametros")]
    public int roomCount = 30;
    public float tileSize = 4f;
    public float wallHeight = 2.5f;
    public float wallThickness = 0.2f;
    public int seed = 0;

    private List<Vector2Int> rooms = new List<Vector2Int>();
    private Vector2Int startTile = Vector2Int.zero;
    private Vector2Int exitTile = Vector2Int.zero;

    void Start()
    {
        if (seed != 0)
        {
            Random.InitState(seed);
        }

        if (roomPrefabs == null || roomPrefabs.Count == 0)
        {
            return;
        }

        GenerateSimpleRandomWalk();
        InstantiateRoomsAndWalls();

        NavMeshSurface surface = FindFirstObjectByType<NavMeshSurface>();
        if (surface != null)
        {
            surface.BuildNavMesh();
        }

        //Hacer habitación inicial
        if (rooms != null && rooms.Count > 0)
        {
            startTile = rooms[0];
            exitTile = rooms[rooms.Count - 1];
        }

        // Spawn de enemigos
        SpawnEnemies();

        // Colocar player y salida
        MovePlayerAtStartRoom();
        SpawnExit();
    }

    void GenerateSimpleRandomWalk()
    {
        rooms.Clear();
        Vector2Int current = Vector2Int.zero;
        rooms.Add(current);

        int attempts = 0;
        while (rooms.Count < roomCount && attempts < roomCount * 10)
        {
            attempts++;
            int direccion = Random.Range(0, 4);

            if (direccion == 0)
            {
                current += Vector2Int.up;
            }
            else if (direccion == 1)
            {
                current += Vector2Int.down;
            }
            else if (direccion == 2)
            {
                current += Vector2Int.left;
            }
            else
            {
                current += Vector2Int.right;
            }

            if (!RoomsContains(current))
            {
                rooms.Add(current);
            }
        }
    }

    bool RoomsContains(Vector2Int pos)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i] == pos)
            {
                return true;
            }
        }
        return false;
    }

    void InstantiateRoomsAndWalls()
    {
        foreach (Vector2Int tile in rooms)
        {
            Vector3 tilePosition = new Vector3(tile.x * tileSize, 0f, tile.y * tileSize);

            int index = Random.Range(0, roomPrefabs.Count);
            GameObject currentRoomPrefab = roomPrefabs[index];

            if (currentRoomPrefab != null)
            {
                Instantiate(currentRoomPrefab, tilePosition, Quaternion.identity, this.transform);
            }

            if (roomFloorPrefab != null)
            {
                Vector3 floorPosition = tilePosition + Vector3.down * 0.49f;
                Instantiate(roomFloorPrefab, floorPosition, Quaternion.identity, this.transform);
            }
        }

        foreach (Vector2Int tile in rooms)
        {
            Vector2Int upWall = tile + Vector2Int.up;
            Vector2Int downWall = tile + Vector2Int.down;
            Vector2Int rightWall = tile + Vector2Int.right;
            Vector2Int leftWall = tile + Vector2Int.left;

            Vector3 basePosition = new Vector3(tile.x * tileSize, 0f, tile.y * tileSize);

            if (!RoomsContains(upWall))
            {
                Vector3 pos = basePosition + new Vector3(0f, wallHeight / 2f, tileSize / 2f);
                Vector3 scale = new Vector3(tileSize, wallHeight, wallThickness);
                InstantiateWall(pos, scale);
            }

            if (!RoomsContains(downWall))
            {
                Vector3 pos = basePosition + new Vector3(0f, wallHeight / 2f, -tileSize / 2f);
                Vector3 scale = new Vector3(tileSize, wallHeight, wallThickness);
                InstantiateWall(pos, scale);
            }

            if (!RoomsContains(rightWall))
            {
                Vector3 pos = basePosition + new Vector3(tileSize / 2f, wallHeight / 2f, 0f);
                Vector3 scale = new Vector3(wallThickness, wallHeight, tileSize);
                InstantiateWall(pos, scale);
            }

            if (!RoomsContains(leftWall))
            {
                Vector3 pos = basePosition + new Vector3(-tileSize / 2f, wallHeight / 2f, 0f);
                Vector3 scale = new Vector3(wallThickness, wallHeight, tileSize);
                InstantiateWall(pos, scale);
            }
        }
    }

    void InstantiateWall(Vector3 position, Vector3 scale)
    {
        if (roomWallPrefab == null)
        {
            return;
        }
        GameObject wall = Instantiate(roomWallPrefab, position, Quaternion.identity, this.transform);
        wall.transform.localScale = scale;
    }


    void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            return;
        }
        if (rooms == null || rooms.Count == 0)
        {
            return;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            Vector2Int tile = rooms[i];

            if (tile == startTile || tile == exitTile)
            {
                continue;
            }

            if (Random.value > enemyProbability)
            {
                continue;
            }

            int spawnCount = Mathf.Clamp(Random.Range(1, maxEnemiesPerRoom + 1), 1, maxEnemiesPerRoom);

            for (int e = 0; e < spawnCount; e++)
            {
                int index = Random.Range(0, enemyPrefabs.Count);
                GameObject currrentEnemy = enemyPrefabs[index];
                if (currrentEnemy == null)
                {
                    continue;
                }

                Vector2 randomCircle = Random.insideUnitCircle * (tileSize * 0.3f);
                Vector3 enemySpawnPosition = new Vector3(tile.x * tileSize + randomCircle.x, 0f, tile.y * tileSize + randomCircle.y);
                enemySpawnPosition += Vector3.up * 0.5f;

                Vector3 finalEnemyPosition = FindNearestNavMeshPosition(enemySpawnPosition, tileSize * 0.6f);

                GameObject enemy = Instantiate(currrentEnemy, finalEnemyPosition, Quaternion.identity, this.transform);

                NavMeshAgent agentComponent = enemy.GetComponent<NavMeshAgent>();
                if (agentComponent != null)
                {
                    bool warped = agentComponent.Warp(finalEnemyPosition);
                    if (!warped)
                    {
                        enemy.transform.position = finalEnemyPosition;
                    }
                }
                else
                {
                    enemy.transform.position = finalEnemyPosition;
                }
            }
        }
    }

    Vector3 FindNearestNavMeshPosition(Vector3 samplePos, float maxDistance)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(samplePos, out hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return samplePos;
    }


    void MovePlayerAtStartRoom()
    {
        if (playerReference == null || rooms.Count == 0)
        {
            return;
        }

        Vector3 startTilePosition = new Vector3(startTile.x * tileSize, 0f, startTile.y * tileSize);
        Vector3 warpPosition = startTilePosition + Vector3.up * 0.5f;
        warpPosition = FindNearestNavMeshPosition(warpPosition, tileSize * 0.6f);

        NavMeshAgent agentComponent = playerReference.GetComponent<NavMeshAgent>();
        if (agentComponent != null)
        {
            bool warped = agentComponent.Warp(warpPosition);
            if (!warped)
            {
                playerReference.transform.position = warpPosition;
            }
        }
        else
        {
            playerReference.transform.position = warpPosition + Vector3.up * 0.5f;
        }
    }

    void SpawnExit()
    {
        if (roomExitPrefab == null || rooms.Count == 0)
        {
            return;
        }

        Vector3 exitPosition = new Vector3(exitTile.x * tileSize, 0f, exitTile.y * tileSize);
        Vector3 exitSpawnPosition = exitPosition + Vector3.up * 0.5f;
        exitSpawnPosition = FindNearestNavMeshPosition(exitSpawnPosition, tileSize * 0.6f);

        Instantiate(roomExitPrefab, exitSpawnPosition, Quaternion.identity, this.transform);
    }

}
