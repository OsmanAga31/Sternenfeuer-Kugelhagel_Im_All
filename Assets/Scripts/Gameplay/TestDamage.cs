using UnityEngine;
using FishNet.Object;

public class TestDamage : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (IsServerInitialized)
            DoDamge(other);
    }

    [Server]
    private void DoDamge(Collider other)
    {
        other.GetComponent<IDamagable>()?.Damage(100);
    }

}
