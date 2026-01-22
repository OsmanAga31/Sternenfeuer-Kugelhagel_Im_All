using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    private PlayerController localPlayer;
    private bool isInitialized = false;
    private int retryCount = 0;
    private const int MAX_RETRIES = 20;


    private void Start()
    {
        if(scoreText == null)
            scoreText = GetComponentInChildren<TextMeshProUGUI>();
        //    scoreText = transform.parent.GetComponentInChildren<TextMeshProUGUI>();

        //if (ScoreManager.Instance != null)
        //    ScoreManager.Instance.OnPlayerScoreChanged += OnPlayerScoreChanged;

        // show initial text
        if(scoreText != null )
            scoreText.text = "Score: 0";
        
        // try to find local player
        FindLocalPlayer();
    }

    private void FindLocalPlayer()
    {
        retryCount++;

        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach(PlayerController player in allPlayers)
        {
            if (player.IsOwner)
            {
                localPlayer = player;
                Debug.Log($"[ScoreUI] Local player found: {localPlayer.PlayerName} (ID: {localPlayer.PlayerOwnerId}");

                // reset retry counter
                retryCount = 0;
                InitializeScoreDisplay();
                return;
            }
        }

        if(localPlayer == null && retryCount < MAX_RETRIES)
        {
            Debug.Log($"[ScoreUI] Local player not found yet, retrying... ({retryCount}/{MAX_RETRIES})");
            Invoke(nameof(FindLocalPlayer), 0.5f);
        }
        else if (retryCount >=  MAX_RETRIES)
        {
            Debug.LogError("[ScoreUI] Failed to find local player after max retries!");
        }
    }

    private void InitializeScoreDisplay()
    {
        if (isInitialized)
        {
            Debug.Log("[ScoreUI] Already initialized, skipping");
            return;
        }

        // wait till score manager is ready
        if (ScoreManager.Instance == null)
        {
            Debug.Log("[ScoreUI] ScoreManager not ready, waiting...");
            Invoke(nameof(InitializeScoreDisplay), 0.2f);
            return;
        }
        ScoreManager.Instance.OnPlayerScoreChanged += OnPlayerScoreChanged;

        UpdateScoreDisplay();

        isInitialized = true;
        Debug.Log("[ScoreUI] Initialized and subscribed to score changes");
    }

    //private void Update()
    //{
    //    //UpdateScore(ScoreManager.Instance?.BaseScore ?? 0);
    //    UpdateScoreDisplay();
    //}

    private void OnPlayerScoreChanged(int playerId, int newScore)
    {
        // only update if its local player
        if (localPlayer != null && playerId == localPlayer.PlayerOwnerId)
        {
            Debug.Log($"[ScoreUI] Score changed for local player: {newScore}");
            UpdateScoreDisplay();
        }
        else
        {
            Debug.Log($"[ScoreUI] Score changed for different player (ID: {playerId}): {newScore} (local player ID: {localPlayer?.PlayerOwnerId ?? -1})");
        }
    }

    // main method for multiplayer score count
    private void UpdateScoreDisplay()
    {
        if (scoreText == null || localPlayer == null)
        {
            Debug.LogWarning("[ScoreUI] scoreText is null!");
            return;
        }

        if (localPlayer == null)
        {
            Debug.LogWarning("[ScoreUI] localPlayer is null!");
            scoreText.text = "Score: 0";
            return;
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[ScoreUI] ScoreManager.Instance is null!");
            scoreText.text = "Score: 0";
            return;
        }

        //// Holt genau EINEN PlayerController (kein Array!)
        //var player = FindFirstObjectByType<PlayerController>();

        //if (player == null)
        //{
        //    scoreText.text = "Score: 0";
        //    return;
        //}

        //string playerName = !string.IsNullOrEmpty(localPlayer.PlayerName)
        //    ? localPlayer.PlayerName
        //    : $"Player{localPlayer.PlayerOwnerId}";

        int score = ScoreManager.Instance?.GetPlayerScore(localPlayer.PlayerOwnerId) ?? 0;
        scoreText.text = $"Score: {score}";

        Debug.Log($"[ScoreUI] Updated display - Player ID: {localPlayer.PlayerOwnerId}, Score: {score}");
    }

    //private void UpdateScore(int score)
    //{
    //    if (scoreText != null)
    //        scoreText.text = $"Score: {score}";
    //}

    private void OnDestroy()
    {
        // Clean up
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnPlayerScoreChanged -= OnPlayerScoreChanged;
            Debug.Log("[ScoreUI] Unsubscribed from score changes");
        }
    }
}