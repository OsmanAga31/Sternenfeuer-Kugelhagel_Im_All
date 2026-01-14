using System;
using System.Collections.Generic;
using UnityEngine;

/// Tutorial by Boundfox Studios https://www.youtube.com/watch?v=6kjPUmvvLoM 

// Von Naomi
[Serializable]
public class ObjectPool 
{
    public GameObject ObjectToPool; // wich object needs to be pooled 
    public int PrewarmAmount; // how many instances we want to pre-create
    public bool canExpand; // if object pool is expandable 
    public string Name; // name of object pool

    [NonSerialized]
    public List<GameObject> Items = new List<GameObject>(); // list of all created game objects 
}
