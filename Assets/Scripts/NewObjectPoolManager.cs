using FishNet;
using FishNet.Object;
using FishNet.Utility.Performance;
using System;
using UnityEngine;

public class NewObjectPoolManager : NetworkBehaviour
{
    public GameObject[] ObjectsToSpawn;
    [SerializeField] private int[] prewarmAmount = {500, 50};

    public static NewObjectPoolManager Instance;

    public override void OnStartServer()
    {
        Instance = this; // creates an instance of ObjectPoolManager
        PrewarmPools();
    }

    void PrewarmPools()
    {
        DefaultObjectPool pool = InstanceFinder.NetworkManager.GetComponent<DefaultObjectPool>();
        int cnt = 0;
        foreach (GameObject obj in ObjectsToSpawn)
        {
            pool.CacheObjects(obj.GetComponent<NetworkObject>(), prewarmAmount[cnt++], IsServerInitialized);
        }

    }

    public NetworkObject getObject(PoolObjectType type)
    {
        int index = (int)type;
        return ObjectsToSpawn[index].GetComponent<NetworkObject>(); 
    }

}


public enum PoolObjectType
{
    Bullet,
    Enemy1
}