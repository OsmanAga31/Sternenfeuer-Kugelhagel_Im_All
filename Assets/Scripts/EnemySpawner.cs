using UnityEngine;
using FishNet.Object;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using System.Xml.Serialization;

public class EnemySpawner : NetworkBehaviour
{
    public static EnemySpawner Instance;

    public bool isGameOver = false;

    [Header("UI Elements")]
    [SerializeField] private GameObject victoryMessage;

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

    List<Coroutine> routines = new List<Coroutine>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (IsServerInitialized && Instance == null)
            Instance = this;

        victoryMessage.SetActive(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        victoryMessage.SetActive(false);
    }

    [Server]
    public void StartGame()
    {
        if (minSpawnRadius > spawnRadius)
            spawnRadius = minSpawnRadius;
        routines.Add(StartCoroutine(CheckForPlayers()));
        isGameOver = false;
        victoryMessage.SetActive(false);
    }

    [Server]
    public void StopGame(bool disableScreen = true)
    {
        foreach (Coroutine routine in routines)
        {
            if (routine != null)
                StopCoroutine(routine);
        }
        routines.Clear();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Enemy1 AnnaMay = enemy.GetComponent<Enemy1>();
            if (AnnaMay != null)
            {
                AnnaMay.Die(null);
            }
        }

        GameObject[] bullettos = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bulleto in bullettos)
        {
            Bullet bul = bulleto.GetComponent<Bullet>();
            if (bul != null)
            {
                bul.DeactivateBullet();
            }
        }


        isGameOver = false;
        if (!disableScreen)
            return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.Heal(pc.MaxHP);
                pc.SaveScore();
                ScoreManager.Instance.ResetPlayerScore(pc.PlayerOwnerId);
            }
        }

        victoryMessage.SetActive(false);

    }

    private IEnumerator CheckForPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        while (players.Length < minPlayersToStart)
        {
            players = GameObject.FindGameObjectsWithTag("Player");
            yield return null;
        }

        routines.Add(StartCoroutine(SpawnDelayed()));
        routines.Add(StartCoroutine(RaidEvent()));
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
        for (int i = 0; i < spawnCountEnemies && !isGameOver; i++)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy(false);
        }
        ShowVictoryMessage();
    }

    [ObserversRpc]
    private void ShowVictoryMessage()
    {
        victoryMessage.SetActive(false);

        if (!isGameOver)
            victoryMessage.SetActive(true);

        if (IsServerInitialized)
            StopGame(disableScreen:false);
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
