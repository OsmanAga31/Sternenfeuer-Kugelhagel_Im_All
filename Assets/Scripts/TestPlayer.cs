using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class NewNetworkBehaviourTemplate : NetworkBehaviour, IDamagable
{
    private readonly SyncVar<int> health = new SyncVar<int>(100);

    void Start()
    {
        health.OnChange += OnHealthChange;
    }
    private void OnHealthChange(int previous, int current, bool asServer)
    {
        Debug.Log("Health changed from " + previous + " to " + current);
    }

    public void Damage(int damageAmount)
    {
        health.Value -= damageAmount;
    }

}
