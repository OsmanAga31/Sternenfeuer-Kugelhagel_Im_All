using UnityEngine;
using FishNet.Object;
using System.Collections;
using System;
using FishNet.Object.Synchronizing;

public class Enemy1 : NetworkBehaviour, IDamagable
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootDelay;
    [SerializeField, Min(2)] private float speedRange;

    private Coroutine shooter;
    private int speed;
    private bool isSubscribedToOnTick_GetPlayers = false;
    private bool isSubscribedToOnTick_MoveTowardsPlayer = false;

    private GameObject playerLocation;

    [Header("Enemy1 Stats")]
    private const int maxHealth = 100;
    [SerializeField] private readonly SyncVar<int> health = new SyncVar<int>(100);


    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!isSubscribedToOnTick_GetPlayers)
        {
            TimeManager.OnTick += GetClosestPlayers;
            isSubscribedToOnTick_GetPlayers = true;
        }
        speed = UnityEngine.Random.Range(2, (int)speedRange);

        Debug.Log("Server Initialized - Enemy1");
    }

    private void OnEnable()
    {
        speed = UnityEngine.Random.Range(2, (int)speedRange);

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
    private void Die()
    {
        TimeManager.OnTick -= MoveOnTick;
        if (shooter != null)
            StopCoroutine(shooter);
        Despawn(DespawnType.Pool);
        Debug.Log("Enemy1 Destroyed");
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

            Spawn(poolBullet);

            // pass shooter type enemy to ensure its bullets only hit player 
            Bullet bullet = poolBullet.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.ShootBullet(5, speed + 2f, 5f, ShooterType.Enemy);
            }
        }
    }

    private void Start()
    {
        health.OnChange += OnHealthChanged;
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

    [Server]
    public void Damage(int dmg)
    {
        health.Value -= dmg;
        Debug.Log("Enemy1 Health: " + health.Value);
        if (health.Value <= 0)
        {
            Die();
        }
    }

    private void OnHealthChanged(int previous, int current, bool asServer)
    {
        // This method is called on both server and clients when health changes.
        Debug.Log($"Enemy1 Health changed from {previous} to {current}. Health: {current}/{maxHealth} which is perc: {(current * 100) / maxHealth}%");
    }

    #endregion

}
