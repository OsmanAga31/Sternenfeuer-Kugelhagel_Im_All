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
    public NetworkObject shooterObject { get; private set; }
    private Coroutine bul;
    private bool isOnTickSubscribed = false;

    private void OnTick()
    {
        transform.Translate((speed * (float)TimeManager.TickDelta) * Vector3.forward, Space.Self);
    }

    [Server]
    public void ShootBullet(int damage, float speed, float lifeTime, ShooterType shooterType, NetworkObject shooterObj = null)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[Bullet] ShootBullet called on non-server!");
            return;
        }

        this.damage = damage;
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.shooterType = shooterType;
        this.shooterObject = shooterObj;

        Debug.Log($"[Bullet] ShootBullet - RECEIVED:");
        Debug.Log($"  - ShooterType: {shooterType}");
        Debug.Log($"  - Damage: {damage}");
        Debug.Log($"  - shooterObj parameter: {(shooterObj != null ? "NOT NULL" : "NULL")}");

        if (shooterObj != null)
        {
            Debug.Log($"  - shooterObj.OwnerId: {shooterObj.OwnerId}");
        }

        Debug.Log($"  - this.shooterObject (stored): {(this.shooterObject != null ? "NOT NULL" : "NULL")}");

        if (this.shooterObject != null)
        {
            Debug.Log($"  - this.shooterObject.OwnerId: {this.shooterObject.OwnerId}");
        }

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

        Debug.Log($"[Bullet] ========== HIT DETECTED ==========");
        Debug.Log($"[Bullet] Hit object: {other.name}");
        Debug.Log($"[Bullet] Hit tag: {other.tag}");
        Debug.Log($"[Bullet] Bullet ShooterType: {shooterType}");
        Debug.Log($"[Bullet] Stored shooterObject: {(shooterObject != null ? $"EXISTS (OwnerId: {shooterObject.OwnerId})" : "NULL")}");

        // based on shooter type check which targets can get hit
        bool shouldDamage = false;

        if (shooterType == ShooterType.Player)
        {
            // player bullets only hit enemies
            if (other.CompareTag("Enemy"))
            {
                shouldDamage = true;
                Debug.Log($"[Bullet] Player bullet hit Enemy - SHOULD DAMAGE");
            }
        }
        else if (shooterType == ShooterType.Enemy)
        {
            // enemy bullets only hit player
            if (other.CompareTag("Player"))
            {
                shouldDamage = true;
                Debug.Log($"[Bullet] Enemy bullet hit Player - SHOULD DAMAGE");
            }
        }

        if (shouldDamage)

        {
            Debug.Log($"[Bullet] Attempting to damage {other.name}...");
            Debug.Log($"[Bullet] Passing shooterObject: {(shooterObject != null ? $"ID {shooterObject.OwnerId}" : "NULL")}");

            IDamagable damageable = other.GetComponent<IDamagable>();
            if (damageable != null)
            {
                Debug.Log($"[Bullet] IDamagable found, calling Damage({damage}, shooter)");
                damageable.Damage(damage, shooterObject);
            }
            else
            {
                Debug.LogError($"[Bullet] No IDamagable component on {other.name}!");
            }

            DeactivateBullet();
        }
        else
        {
            Debug.Log($"[Bullet] Should NOT damage (wrong target type)");
        }
        {
            //Debug.Log($"[Bullet] Hit {other.name}! Passing shooter: {(shooterObject != null ? $"ID {shooterObject.OwnerId}" : "NULL")}");
            //other.GetComponent<IDamagable>()?.Damage(damage, shooterObject);
            //DeactivateBullet();
        }
    }
    

    [Server]
    private IEnumerator BulletLife()
    {
        yield return new WaitForSeconds(lifeTime);
        DeactivateBullet();
    }

    [Server]
    public void DeactivateBullet()
    {
        if (!IsServerInitialized)
            return;

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
