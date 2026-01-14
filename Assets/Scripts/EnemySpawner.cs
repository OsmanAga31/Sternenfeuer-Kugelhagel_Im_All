using UnityEngine;
using FishNet.Object;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(SpawnDelayed());
    }

    [Server]
    private void SpawnEnemy()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        Spawn(enemyInstance);
        Debug.Log("Enemy spawned on server. " + enemyInstance.GetInstanceID());
    }

    private IEnumerator SpawnDelayed()
    {
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(1f);
            SpawnEnemy();
        }
    }

}
