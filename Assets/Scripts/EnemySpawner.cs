using UnityEngine;
using FishNet.Object;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    [SerializeField] private int minPlayersToStart = 1;
    [SerializeField] private float spawnInterval = 1f;
    private float spawnRadius = 15f;
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
        Vector3 spawnPosition = new Vector3(Random.Range(-spawnRadius, spawnRadius), 0.5f, Random.Range(-spawnRadius, spawnRadius));

        GameObject poolEnemy = ObjectPoolManager.Instance.Get(targetPoolName);
        Spawn(poolEnemy);

        if (poolEnemy == null)
        {
            Debug.LogWarning("No enemy available in pool.");
            return;
        }

        poolEnemy.transform.position = spawnPosition;
        poolEnemy.SetActive(true);

        Debug.Log("Enemy spawned on server. " + poolEnemy.name);
    }

    private IEnumerator SpawnDelayed()
    {
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

}
