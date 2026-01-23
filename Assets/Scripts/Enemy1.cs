using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Enemy1 : NetworkBehaviour, IDamagable
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootDelay;
    [SerializeField, Min(2)] private float speedRange;
    [SerializeField] private float bulletLifeTime = 10f;

    private Coroutine shooter;
    private bool isAlive = true;
    private float speed;
    private bool isSubscribedToOnTick_GetPlayers = false;
    private bool isSubscribedToOnTick_MoveTowardsPlayer = false;

    private GameObject playerLocation;

    [Header("Enemy Stats")]
    [SerializeField] private int damage = 10;
    [SerializeField] private const int maxHealth = 100;
    [SerializeField] private readonly SyncVar<int> health = new();

    [Header("Boss Settings")]
    [SerializeField] private bool isBoss;

    [SerializeField] private float bossSpeedMultiplier = 0.5f;
    [SerializeField] private float bossHealthMultiplier = 10f;
    [SerializeField] private float bossScaleMultiplier = 3f;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!isSubscribedToOnTick_GetPlayers)
        {
            TimeManager.OnTick += GetClosestPlayers;
            isSubscribedToOnTick_GetPlayers = true;
        }
        speed = UnityEngine.Random.Range(2, speedRange);

        if (isBoss)
        {
            speed = (speed * bossSpeedMultiplier);
            health.Value = (int)(maxHealth * bossHealthMultiplier);
            transform.localScale = transform.localScale * bossScaleMultiplier;
        }
        else
        {
            health.Value = maxHealth;
        }

        Debug.Log("Server Initialized - Enemy1");
    }

    private void OnEnable()
    {
        speed = UnityEngine.Random.Range(2, speedRange);
        isAlive = true;

    }

    [Server]
    private void GetClosestPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0)
            return;

        Array.Sort(players, (a, b) => {

            float distA = Vector3.Distance(a.transform.position, transform.position);
            float distB = Vector3.Distance(b.transform.position, transform.position);
            return distA.CompareTo(distB);

        });

        playerLocation = players[0];

        shooter = StartCoroutine(Shoot());

        if (isSubscribedToOnTick_GetPlayers)
        {
            TimeManager.OnTick -= GetClosestPlayers;
            isSubscribedToOnTick_GetPlayers = false;
        }
        if (!isSubscribedToOnTick_MoveTowardsPlayer)
        {
            TimeManager.OnTick += MoveOnTick;
            isSubscribedToOnTick_MoveTowardsPlayer = true;
        }



    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (isSubscribedToOnTick_GetPlayers)
        {
            TimeManager.OnTick -= GetClosestPlayers;
            isSubscribedToOnTick_GetPlayers = false;
        }
        if (isSubscribedToOnTick_MoveTowardsPlayer)
        {
            TimeManager.OnTick -= MoveOnTick;
            isSubscribedToOnTick_MoveTowardsPlayer = false;
        }

        Debug.Log("Server Stopped - Enemy1");
    }

    // nur wenn Enemy wirklich stirbt und nicht bei OnStopServer
    public void Die(NetworkObject shooter)
    {
        //TimeManager.OnTick -= MoveOnTick;
        //if (shooter != null)
        //    StopCoroutine(shooter);
        //Despawn(DespawnType.Pool);
        //Debug.Log("Enemy1 Destroyed");
        //int points = ScoreManager.Instance.GetEnemyPoints(isBoss);
        //ScoreManager.Instance.AddEnemyPointsServer(points);


        //TimeManager.OnTick -= MoveOnTick;
        //if (shooter != null) StopCoroutine(shooter);

        //int points = isBoss ? 300 : 100;
        //if (ScoreManager.Instance != null)
        //{
        //    ScoreManager.Instance.AddEnemyPointsServer(points);
        //    Debug.Log($"Enemy besiegt! +{points} Punkte (Score: {ScoreManager.Instance.BaseScore})");
        //}
        //else
        //{
        //    Debug.LogWarning("ScoreManager nicht bereit oder nicht Server!");
        //}

        //isAlive = false;
        //Despawn(DespawnType.Pool);

        TimeManager.OnTick -= MoveOnTick;
        if (shooter != null)
        {
            if (shooter.OwnerId != int.MaxValue)  // Gültiger Owner?
            {
                int points = isBoss ? 300 : 100;
                ScoreManager.Instance.AddPointsToPlayerServer(shooter.OwnerId, points);
                Debug.Log($"Enemy besiegt von Player {shooter.OwnerId}! {points} Punkte");
            }
        }
        isAlive = false;
        Despawn(DespawnType.Pool);
    }



[Server]
    private IEnumerator Shoot()
    {
        while (true) { 
            yield return new WaitForSeconds(shootDelay);

            Vector3 spawnPosition = transform.position + transform.forward;

            NetworkObject poolBullet = NetworkManager.GetPooledInstantiated(NewObjectPoolManager.Instance.getObject(PoolObjectType.Bullet), true);

            poolBullet.transform.position = spawnPosition;
            poolBullet.transform.rotation = transform.rotation;
            if (isBoss)
                poolBullet.transform.localScale *= bossScaleMultiplier;

            Spawn(poolBullet);

            poolBullet.gameObject.GetComponent<Bullet>().ShootBullet(
                damage,
                speed +2f,
                bulletLifeTime,
                ShooterType.Enemy,
                //null
                this.NetworkObject
                );
        }
    }

    [Server]
    private void MoveOnTick()
    {
        //Debug.Log(name + " Moving towards player");
        Vector3 distance = playerLocation.transform.position - transform.position;

        if (distance.sqrMagnitude >= 1f)
            MoveToPosition();
    }

    [Server]
    private void MoveToPosition()
    {
        //transform.Translate(pos + ((speed * (float)TimeManager.TickDelta) * Vector3.one), Space.World);
        transform.LookAt(playerLocation.transform);
        transform.Translate(((speed * (float)TimeManager.TickDelta) * Vector3.forward), Space.Self);
    }

    #region Health / IDamagable Implementation

    public void Damage(int dmg, NetworkObject shooter = null)
    {
        if (!IsServerInitialized || !isAlive)
            return;
        health.Value -= dmg;
        //Debug.Log("Enemy1 Health: " + health.Value);
        if (health.Value <= 0)
        {
            Die(shooter);
        }
    }

    #endregion

}
