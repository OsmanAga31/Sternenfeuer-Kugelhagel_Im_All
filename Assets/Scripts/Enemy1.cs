using UnityEngine;
using FishNet.Object;
using System.Collections;
using System;
using FishNet.Object.Synchronizing;

public class Enemy1 : NetworkBehaviour, IDamagable
{
    private int pos = 4;
    private int posCount = 0;
    private Vector3[] posXs;

    private Coroutine shooter;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootDelay;
    [SerializeField] private int posINdex = 0;
    [SerializeField, Min(2)] private float speedRange;
    private int speed;

    private GameObject playerLocation;

    [Header("Enemy1 Stats")]
    private const int maxHealth = 100;
    [SerializeField] private readonly SyncVar<int> health = new SyncVar<int>(100);


    public override void OnStartServer()
    {
        base.OnStartServer();

        speed = UnityEngine.Random.Range(2, (int)speedRange);
        posXs = new Vector3[] { new Vector3(pos, 0.5f, 0), new Vector3(-pos, 0.5f, 0), new Vector3(0, 0.5f, pos), new Vector3(0, 0.5f, -pos) };

        TimeManager.OnTick += MoveOnTick;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Array.Sort(players, (a, b) => {
            
            float distA = Vector3.Distance(a.transform.position, transform.position);
            float distB = Vector3.Distance(b.transform.position, transform.position);
            return distA.CompareTo(distB);

        });

        if (players.Length > 0)
        {
            playerLocation = players[0];
        }

        shooter = StartCoroutine(Shoot());

        Debug.Log("Server Initialized - Enemy1");


    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        TimeManager.OnTick -= MoveOnTick;

        Die();

        Debug.Log("Server Stopped - Enemy1");
    }

    private void Die()
    {
        TimeManager.OnTick -= MoveOnTick;
        if (shooter != null)
            StopCoroutine(shooter);
        Despawn(DespawnType.Destroy);
        Destroy(gameObject);
        Debug.Log("Enemy1 Destroyed");
    }

    private IEnumerator Shoot()
    {
        while (true) { 
            yield return new WaitForSeconds(shootDelay);
            GameObject bullet = Instantiate(bulletPrefab, transform.position + transform.forward, transform.rotation);
            Spawn(bullet);
            bullet.GetComponent<Bullet>().ShootBullet(5, 0.2f, 5f);
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
        //Vector3 distance = posXs[posINdex] - transform.position;
        Vector3 distance = playerLocation.transform.position - transform.position;

        if (distance.sqrMagnitude >= 1f)
            MoveToPosition();
        //Debug.Log("Distance to target: " + distance.sqrMagnitude);
        //if (distance.sqrMagnitude < 0.5f)
        //{
        //    posINdex = ++posINdex % posXs.Length;
        //}
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
