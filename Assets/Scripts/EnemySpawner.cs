using UnityEngine;
using FishNet.Object;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    [SerializeField] private int minPlayersToStart = 1;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float spawnRadius = 50f;
    [SerializeField] private int spawnCountEnemies = 10;
    private string targetPoolName = "Enemies";

    public override void OnStartServer()
    {
        base.OnStartServer();
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
        Vector3 spawnPosition = new Vector3(Random.Range(-spawnRadius, spawnRadius), 0f, Random.Range(-spawnRadius, spawnRadius));

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
