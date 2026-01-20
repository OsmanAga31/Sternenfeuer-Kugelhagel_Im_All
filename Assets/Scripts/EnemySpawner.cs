using UnityEngine;
using FishNet.Object;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject enemyBossPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private int minPlayersToStart = 1;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float minSpawnRadius = 25f;
    [SerializeField] private float spawnRadius = 50f;
    [SerializeField] private int spawnCountEnemies = 10;

    [Header("Raid & Boss Settings")]
    [Tooltip("Interval in seconds for raid events")]
    [SerializeField] private float raidInterval = 60f;
    [Tooltip("Number of enemies to spawn during a raid event. Not including the Boss.")]
    [SerializeField] private int raidEnemyCount = 5;

    private float spawnX;
    private float spawnZ;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (minSpawnRadius > spawnRadius)
            spawnRadius = minSpawnRadius;
        StartCoroutine(CheckForPlayers());
    }

    private IEnumerator CheckForPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        while (players.Length < minPlayersToStart)
        {
            players = GameObject.FindGameObjectsWithTag("Player");
            yield return null;
        }

        StartCoroutine(SpawnDelayed());
        StartCoroutine(RaidEvent());
    }

    [Server]
    private void SpawnEnemy(bool isBoss)
    {
        // Spawn at random position within spawn radius in a ring. Code by A.I. (Only the spawn position)
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnRadius, spawnRadius);

        spawnX = Mathf.Cos(angle) * distance;
        spawnZ = Mathf.Sin(angle) * distance;

        Vector3 spawnPosition = new Vector3(spawnX, 0f, spawnZ);
        NetworkObject poolEnemy;

        if (isBoss)
        {
            poolEnemy = NetworkManager.GetPooledInstantiated(NewObjectPoolManager.Instance.getObject(PoolObjectType.EnemyBoss1), true);
        }
        else
        {
            poolEnemy = NetworkManager.GetPooledInstantiated(NewObjectPoolManager.Instance.getObject(PoolObjectType.Enemy1), true);
        }
        
        poolEnemy.transform.position = spawnPosition;
        Spawn(poolEnemy);
        //Debug.Log("Enemy spawned on server. " + poolEnemy.name);
    }

    private IEnumerator SpawnDelayed()
    {
        for (int i = 0; i < spawnCountEnemies; i++)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy(false);
        }
    }

    private IEnumerator RaidEvent()
    {
        while (true)
        {
            yield return new WaitForSeconds(raidInterval);
            
            for (int i = 0; i < raidEnemyCount; i++)
            {
                SpawnEnemy(false);
            }
            SpawnEnemy(true); // Spawn Boss


        }
    }

}
