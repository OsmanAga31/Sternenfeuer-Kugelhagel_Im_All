using UnityEngine;
using FishNet.Object;
using System.Collections;
using Unity.VisualScripting.Dependencies.Sqlite;
using System;

public class Enemy1 : NetworkBehaviour
{
    private int pos = 4;
    private int posCount = 0;
    private Vector3[] posXs;

    [SerializeField] private float daDlay = 4f;

    [SerializeField] private int posINdex = 0;

    [SerializeField] private float speed = 0.1f;

    public override void OnStartServer()
    {
        base.OnStartServer();
        posXs = new Vector3[] { new Vector3(pos, 0.5f, 0), new Vector3(-pos, 0.5f, 0), new Vector3(0, 0.5f, pos), new Vector3(0, 0.5f, -pos) };

        TimeManager.OnTick += MoveOnTick;

        Debug.Log("Server Initialized - Enemy1");


    }

    [Server]
    private void MoveOnTick()
    {
        Vector3 distance = posXs[posINdex] - transform.position;
        
        MoveToPosition(distance.normalized);
        Debug.Log("Distance to target: " + distance.sqrMagnitude);
        if (distance.sqrMagnitude < 0.5f)
        {
            posINdex = ++posINdex % posXs.Length;
        }
    }

    [Server]
    private void MoveToPosition(Vector3 pos)
    {
        transform.Translate(pos + ((speed * (float)TimeManager.TickDelta) * Vector3.one), Space.World);
    }

}
