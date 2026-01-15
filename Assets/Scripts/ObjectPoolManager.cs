using System.Collections;
using System.Linq;
using UnityEngine;

/// Tutorial by Boundfox Studios https://www.youtube.com/watch?v=6kjPUmvvLoM 

// Von Naomi
public class ObjectPoolManager : MonoBehaviour
{
    public ObjectPool[] Pools; // all existing object pools

    public static ObjectPoolManager Instance;

    private void Start()
    {
        Instance = this; // creates an instance of ObjectPoolManager

        StartCoroutine(PrewarmObject()); // starts coroutine to go through all object pools 
    }

    // this method creates an object
    // and adds it to list of object pool 
    private GameObject CreateInstanceAndAddToPool(ObjectPool pool)
    {
        var instance = Instantiate(pool.ObjectToPool); // instantiate ObjectToPool
        instance.SetActive(false); // and deactivate created object, because it just needs to be in object pool at this moment

        pool.Items.Add(instance); // add to item list of ObjectPool

        return instance;
    }

    // coroutine to go through all object pools
    private IEnumerator PrewarmObject()
    { 
        Debug.Log("Prewarming Object Pools...");

        foreach (var pool in Pools)
        {
            Debug.Log($"Prewarming object pool {pool.Name}...");

            // loop that generates number of objects 
            for(var i = 0; i < pool.PrewarmAmount; i++)
            {
                CreateInstanceAndAddToPool(pool); // create object

                yield return null; // create one single object per frame 
            }
        }

        Debug.Log("Prewarming done.");
    }

    // this method gets objects out of object pools
    public GameObject Get(string name)
    {
        var pool = Pools.FirstOrDefault(p => p.Name == name); // search through pools for pool with chosen name 

        if (pool == null)
        {
            Debug.LogError($"Object pool with name {name} not found!");
            return null;
        }

        var item = pool.Items.FirstOrDefault(i => !i.activeInHierarchy); // if we found chosen pool, search for next not active item

        if(item)
        {
            return item; // return this item
        }

        if(!pool.canExpand)
        {
            return null;
        }
        return CreateInstanceAndAddToPool(pool); // if pool can expand, add another object to pool 
    }
}
