using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Punkte pro Typ")]
    [SerializeField] private int smallEnemyPoints = 100;
    [SerializeField] private int largeEnemyPoints = 150;

    //[SerializeField] private readonly SyncVar<int> baseScore = new (0);

    // dictionary for scores per player
    private readonly SyncDictionary<int, int> playerScores = new SyncDictionary<int, int>();

    private float gameStartTime;
    private bool gameOver;

    // callback with player ID
    public System.Action<int, int> OnPlayerScoreChanged;

    public int GetPlayerScore(int playerId)
    {
        return playerScores.TryGetValue(playerId, out var score) ? score : 0;
    }

    //public int BaseScore => baseScore.Value;
    //public System.Action<int> OnScoreChanged; // for UI
    public int GetEnemyPoints(bool isBoss) => isBoss? largeEnemyPoints: smallEnemyPoints;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Instance = this;
        gameStartTime = Time.time;

        //baseScore.OnChange += BaseScoreChanged;

        playerScores.OnChange += OnScoreDictionaryChanged;

        Debug.Log("ScoreManager gestartet");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if(!IsServerInitialized)
        {
            Instance = this;
            Debug.Log("started ScoreManager (Client)");
        }

        if (!IsServerInitialized)
        {
            playerScores.OnChange += OnScoreDictionaryChanged;
        }
    }

    private void OnScoreDictionaryChanged(SyncDictionaryOperation op, int key, int value, bool asServer)
    {
        if (op == SyncDictionaryOperation.Add || op == SyncDictionaryOperation.Set)
        {
            OnPlayerScoreChanged?.Invoke(key, value);
        }
    }

    [Server/*Rpc(RequireOwnership = false)*/]
    public void AddPointsToPlayerServer(int playerId, int points)
    {
        if (gameOver) return;

        // get current score or 0 if not existing
        int currentScore = playerScores.TryGetValue(playerId, out var score) ? score : 0;

        // update score
        int newScore = currentScore + points;
        playerScores[playerId] = newScore;

        Debug.Log($"[Server] Added {points} points to player {playerId}. New score: {newScore}");

    }

    [Server]
    public void CalculateFinalScore()
    {
        if(gameOver) return; 

        float survivalSeconds = Time.time - gameStartTime;

        //int finalScore = Mathf.RoundToInt(baseScore.Value * survivalSeconds);

        Debug.Log($"=== GAME OVER ===");
        Debug.Log($"Survival Time: {survivalSeconds:F1}s");

        foreach (var kvp in playerScores)
        {
            Debug.Log($"Player {kvp.Key}: {kvp.Value} points");
        }

        gameOver = true;
    }

    //private void BaseScoreChanged(int old, int newVal,bool asServer)
    //{
    //    OnScoreChanged?.Invoke(newVal);
    //}

    private void OnDestroy()
    {
        if(Instance == this)
            Instance = null;
    }
}