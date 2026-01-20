using UnityEngine;
using FishNet.Object;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    [SerializeField] private int minPlayersToStart = 1;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float minSpawnRadius = 25f;
    [SerializeField] private float spawnRadius = 50f;
    [SerializeField] private int spawnCountEnemies = 10;

    private string targetPoolName = "Enemies";
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
    }

    [Server]
    private void SpawnEnemy()
    {
        // Nutze Polarkoordinaten für einen echten Ring
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnRadius, spawnRadius);

        spawnX = Mathf.Cos(angle) * distance;
        spawnZ = Mathf.Sin(angle) * distance;

        Vector3 spawnPosition = new Vector3(spawnX, 0.5f, spawnZ);

        NetworkObject poolEnemy = NetworkManager.GetPooledInstantiated(NewObjectPoolManager.Instance.getObject(PoolObjectType.Enemy1), true);
        poolEnemy.transform.position = spawnPosition;



        Spawn(poolEnemy);

        Debug.Log("Enemy spawned on server. " + poolEnemy.name);
    }

    private IEnumerator SpawnDelayed()
    {
        for (int i = 0; i < spawnCountEnemies; i++)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

}
