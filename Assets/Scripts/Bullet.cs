using UnityEngine;
using FishNet.Object;
using System.Collections;

public enum ShooterType
{
    Player,
    Enemy
}

public class Bullet : NetworkBehaviour
{
    private float speed;
    private float lifeTime;
    private int damage;
    private ShooterType shooterType; // who did shoot?

    private Coroutine bul;
    private bool isOnTickSubscribed = false;

    private void OnTick()
    {
        transform.Translate((speed * (float)TimeManager.TickDelta) * Vector3.forward, Space.Self);
    }

    [Server]
    public void ShootBullet(int damage, float speed, float lifeTime, ShooterType shooterType)
    {
        if (!IsServerInitialized)
            return;

        this.damage = damage;
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.shooterType = shooterType;

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
        
        if (!IsServerInitialized)
            return;

        // based on shooter type check which targets can get hit
        bool shouldDamage = false;

        if(shooterType == ShooterType.Player)
        {
            // player bullets only hit enemies
            if(other.CompareTag("Enemy"))
            {
                shouldDamage = true;
            }
        }
        else if(shooterType == ShooterType.Enemy)
        {
            // enemy bullets only hit player
            if(other.CompareTag("Player"))
            {
                shouldDamage = true;
            }
        }

        if (shouldDamage)
        {
            DeactivateBullet();
            other.GetComponent<IDamagable>()?.Damage(damage);
        }
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
        {
            StopCoroutine(bul);
            bul = null;
        }

        if (isOnTickSubscribed)
        {
            TimeManager.OnTick -= OnTick;
            isOnTickSubscribed = false;
        }

        Despawn(DespawnType.Pool);
    }

}
