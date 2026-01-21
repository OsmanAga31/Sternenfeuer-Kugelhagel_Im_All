using UnityEngine;
using TMPro;
//using FishNet.Connection;
using UnityEngine.SceneManagement;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    private PlayerController localPlayer;
    private bool isInitialized = false;


    private void Start()
    {
        if(scoreText == null)
            scoreText = GetComponentInChildren<TextMeshProUGUI>();
        //    scoreText = transform.parent.GetComponentInChildren<TextMeshProUGUI>();

        //if (ScoreManager.Instance != null)
        //    ScoreManager.Instance.OnPlayerScoreChanged += OnPlayerScoreChanged;

        // try to find local player
        FindLocalPlayer();
    }

    private void FindLocalPlayer()
    {
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach(PlayerController player in allPlayers)
        {
            if (player.IsOwner)
            {
                localPlayer = player;
                InitializeScoreDisplay();
                Debug.Log($"[ScoreUI] Local player found: {localPlayer.PlayerName} (ID: {localPlayer.PlayerOwnerId}");
                return;
            }
        }

        if(localPlayer == null)
        {
            Invoke(nameof(FindLocalPlayer), 0.5f);
        }
    }

    private void InitializeScoreDisplay()
    {
        if (isInitialized) return;

        // wait till score manager is ready
        if (ScoreManager.Instance == null)
        {
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
    }

    // main method for multiplayer score count
    private void UpdateScoreDisplay()
    {
        if (scoreText == null || localPlayer == null) return;

        //// Holt genau EINEN PlayerController (kein Array!)
        //var player = FindFirstObjectByType<PlayerController>();

        //if (player == null)
        //{
        //    scoreText.text = "Score: 0";
        //    return;
        //}

        string playerName = !string.IsNullOrEmpty(localPlayer.PlayerName)
            ? localPlayer.PlayerName
            : $"Player{localPlayer.PlayerOwnerId}";

        int score = ScoreManager.Instance?.GetPlayerScore(localPlayer.PlayerOwnerId) ?? 0;

        scoreText.text = $"{playerName}: {score}";
    }


    //private void UpdateScore(int score)
    //{
    //    if (scoreText != null)
    //        scoreText.text = $"Score: {score}";
    //}

    private void OnDestroy()
    {
        if(ScoreManager.Instance != null)
            ScoreManager.Instance.OnPlayerScoreChanged -= OnPlayerScoreChanged;
    }
}
