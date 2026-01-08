using UnityEngine;
using FishNet.Object;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    public override void OnStartServer()
    {
        base.OnStartServer();
        SpawnEnemy();
    }

    [Server]
    private void SpawnEnemy()
    {
        Vector3 spawnPosition = new Vector3(0, 0.5f, 0);
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        Spawn(enemyInstance);
        Debug.Log("Enemy spawned on server.");
    }

}
