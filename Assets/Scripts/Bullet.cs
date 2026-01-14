using UnityEngine;
using FishNet.Object;
using UnityEditor;
using System.Collections;

public class Bullet : NetworkBehaviour
{
    //[SerializeField] private GameObject bulletPrefab;
    private Vector3 direction;
    private float speed;
    private float lifeTime;
    private int damage;

    private Coroutine bul;


    //public override void OnStartServer()
    //{
    //    base.OnStartServer();
    //}

    private void OnTick()
    {
        transform.Translate((speed * (float)TimeManager.TickDelta) * Vector3.forward, Space.Self);
    }

    [Server]
    public void ShootBullet(int damage, float speed, float lifeTime)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifeTime = lifeTime;

        //bul = StartCoroutine(BulletLife());
        TimeManager.OnTick += OnTick;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServerInitialized || other.tag != "Player")
            return;

        //other.GetComponent<IDamagable>()?.Damage(10);
        DestroyBullet();
    }

    [Server]
    private IEnumerator BulletLife()
    {
        yield return new WaitForSeconds(lifeTime);
        DestroyBullet();
    }

    [Server]
    private void DestroyBullet()
    {
        if (bul != null)
            StopCoroutine(bul);
        TimeManager.OnTick -= OnTick;
        //Destroy(gameObject);
        Despawn(DespawnType.Destroy);
    }

}
