using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
//using System.Collections.Generic;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Punkte pro Typ")]
    [SerializeField] private int smallEnemyPoints = 100;
    [SerializeField] private int largeEnemyPoints = 150;

    //[SerializeField] private readonly SyncVar<int> baseScore = new (0);

    // dictionary for scores per player
    private readonly SyncDictionary<int, int> playerScores = new SyncDictionary<int, int>();

    // track wich players ar alive (can still earn points)
    private readonly SyncDictionary<int, bool> playerAliveStatus = new SyncDictionary<int, bool>();

    // dictionary for spawn times of players
    private readonly SyncDictionary<int, float> playerSpawnTimes = new SyncDictionary<int, float>();

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

    [Server]
    public void RegisterPlayer(int playerId)
    {
        // register new player as alive
        if(!playerAliveStatus.ContainsKey(playerId))
        {
            playerAliveStatus[playerId] = true;
            playerScores[playerId] = 0;
            playerSpawnTimes[playerId] = Time.time;
            Debug.Log($"[ScoreManager] Registered player {playerId} as alive");
        }
    }

    [Server]
    public void AddPointsToPlayerServer(int playerId, int points)
    {
        //if (gameOver) return;

        // check if this specific player is still alive
        if(playerAliveStatus.TryGetValue(playerId, out bool isAlive) && !isAlive)
        {
            Debug.Log($"[ScoreManager] Player {playerId} is dead, cannot earn points");
            return;
        }

        // get current score or 0 if not existing
        int currentScore = playerScores.TryGetValue(playerId, out var score) ? score : 0;

        // update score
        int newScore = currentScore + points;
        playerScores[playerId] = newScore;

        Debug.Log($"[Server] Added {points} points to player {playerId}. New score: {newScore}");
    }

    [Server]
    public void SetPlayerDead(int playerId)
    {
        if(playerAliveStatus.ContainsKey(playerId))
        {
            playerAliveStatus[playerId] = false;

            float survivalSeconds = Time.time - gameStartTime;
            int finalScore = GetPlayerScore(playerId);

            Debug.Log($"=== PLAYER {playerId} DIED ===");
            Debug.Log($"Survival Time: {survivalSeconds:F1}s");
            Debug.Log($"Final Score: {finalScore}");
        }
    }

    // get player dead status
    [Server]
    public bool IsPlayerDead(int playerId)
    {
        return playerAliveStatus.TryGetValue(playerId, out bool isAlive) && !isAlive;
    }

    [Server]
    public void CalculateFinalScore(int playerId)
    {
        ////if(gameOver) return; 

        ////float survivalSeconds = Time.time - gameStartTime;

        ////int finalScore = Mathf.RoundToInt(baseScore.Value * survivalSeconds);

        //float totalGameTime = Time.time - gameStartTime;

        //Debug.Log($"=== GAME OVER - ALL PlAYERS DEAD ===");
        //Debug.Log($"Total Game Time: {totalGameTime:F1}s");

        //foreach (var kvp in playerScores)
        //{
        //    Debug.Log($"Player {kvp.Key}: {kvp.Value} points");
        //}

        //gameOver = true;
        if (!playerScores.ContainsKey(playerId))
        {
            Debug.LogWarning($"Player {playerId} nicht gefunden!");
            return;
        }

        // calculate individual survival time
        float spawnTime = playerSpawnTimes.TryGetValue(playerId, out var time) ? time : gameStartTime;
        float survivalTime = Time.time - spawnTime;

        int currentScore = playerScores[playerId];

        // bonus for survival time
        int timeBonus = Mathf.RoundToInt(survivalTime * 10);

        // final score
        int finalScore = currentScore + timeBonus;

        // update score in dictionary 
        playerScores[playerId] = finalScore;

        Debug.Log($"=== FINAL SCORE FÜR PLAYER {playerId} ===");
        Debug.Log($"Überlebenszeit: {survivalTime:F1}s");
        Debug.Log($"Kill-Punkte: {currentScore}");
        Debug.Log($"Zeit-Bonus: {timeBonus}");
        Debug.Log($"FINAL SCORE: {finalScore}");
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