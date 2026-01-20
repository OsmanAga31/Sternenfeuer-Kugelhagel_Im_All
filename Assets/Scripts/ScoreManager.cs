using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Punkte pro Typ")]
    [SerializeField] private int smallEnemyPoints = 100;
    [SerializeField] private int largeEnemyPoints = 150;

    [SerializeField] private readonly SyncVar<int> baseScore = new (0);
    private float gameStartTime;
    private bool gameOver;

    public int BaseScore => baseScore.Value;
    public System.Action<int> OnScoreChanged; // for UI
    public int GetEnemyPoints(bool isBoss) => isBoss? largeEnemyPoints: smallEnemyPoints;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Instance = this;
        gameStartTime = Time.time;
        baseScore.OnChange += BaseScoreChanged;
        Debug.Log("ScoreManager gestartet");
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddEnemyPointsServer(int points)
    {
        if (gameOver) return;
        baseScore.Value += points;
    }

    [Server]
    public void CalculateFinalScore()
    {
        if(gameOver) return;
        float survivalSeconds = Time.time - gameStartTime;
        int finalScore = Mathf.RoundToInt(baseScore.Value * survivalSeconds);
        Debug.Log($"Game Over! Final Score: {finalScore} (Basis: {baseScore.Value} x {survivalSeconds:F1}s");
        gameOver = true;
    }

    private void BaseScoreChanged(int old, int newVal,bool asServer)
    {
        OnScoreChanged?.Invoke(newVal);
    }
}