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
    private bool isOnTickSubscribed = false;

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

        bul = StartCoroutine(BulletLife());
        if (!isOnTickSubscribed)
        {
            TimeManager.OnTick += OnTick;
            isOnTickSubscribed = true;
        }
    }

    private void OnDisable()
    {
        if (isOnTickSubscribed)
        {
            TimeManager.OnTick -= OnTick;
            isOnTickSubscribed = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServerInitialized || other.tag != "Player")
            return;

        other.GetComponent<IDamagable>()?.Damage(10);
        DeactivateBullet();
    }

    [Server]
    private IEnumerator BulletLife()
    {
        yield return new WaitForSeconds(lifeTime);
        DeactivateBullet();
    }

    [Server]
    private void DeactivateBullet()
    {
        if (bul != null)
            StopCoroutine(bul);
        TimeManager.OnTick -= OnTick;
        gameObject.SetActive(false);
    }

}
