using FishNet.Object;
using UnityEngine;

public interface IDamagable
{
    void Damage(int dmg, NetworkObject shooter = null);
}
