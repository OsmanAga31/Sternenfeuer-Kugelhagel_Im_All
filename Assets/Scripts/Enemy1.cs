using UnityEngine;
using FishNet.Object;
using System.Collections;
using System;
using FishNet.Object.Synchronizing;
using System.Runtime.CompilerServices;

public class Enemy1 : NetworkBehaviour, IDamagable
{
    private int pos = 4;

    private Coroutine shooter;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootDelay;
    [SerializeField, Min(2)] private float speedRange;
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
        Despawn(DespawnType.Destroy);
        //Destroy(gameObject);
        Debug.Log("Enemy1 Destroyed");
    }

    private IEnumerator Shoot()
    {
        while (true) { 
            yield return new WaitForSeconds(shootDelay);
            GameObject bullet = Instantiate(bulletPrefab, transform.position + transform.forward, transform.rotation);
            Spawn(bullet);
            bullet.GetComponent<Bullet>().ShootBullet(5, speed+2f, 5f);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    private void Start()
    {
        health.OnChange += OnHealthChanged;
    }

    [Server]
    private void MoveOnTick()
    {
        Debug.Log(name + " Moving towards player");
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
