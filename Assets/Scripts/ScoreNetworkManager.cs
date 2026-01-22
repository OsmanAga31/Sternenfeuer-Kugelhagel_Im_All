using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using FishNet.Object;       // Wichtig für NetworkBehaviour
using FishNet.Connection;   // Wichtig für NetworkConnection

public class ScoreNetworkManager : NetworkBehaviour
{
    public static ScoreNetworkManager Instance;

    public override void OnStartServer()
    {
        if (Instance == null && IsServerInitialized)
        {
            Instance = this;
        }
    }

    [Header("API Base URL (Server Only)")]
    // Passe diese URL an, wenn du das Spiel veröffentlichst (kein localhost mehr!)
    [SerializeField] private string baseUrl = "http://localhost/Sternenfeuer-Kugelhagel_Im_All/API";

    // --- EVENTS (UI abonniert diese) ---
    public static event Action<string[]> OnHighscoresReceived;
    public static event Action<int> OnMyBestScoreReceived;

    // --- DATENSTRUKTUREN FÜR JSON (Muss exakt zum PHP passen) ---

    // 1. Save Score
    [Serializable] private class SaveRequest { public string name; public int score; }
    [Serializable] private class SaveResponse { public bool ok; public int id; public string error; }

    // 2. Get List
    [Serializable] private class ScoreRow { public int id; public string name; public int score; public string created_at; }
    [Serializable] private class GetScoreResponse { public bool ok; public ScoreRow[] players; public string error; }

    // 3. Get My Best
    [Serializable] private class MyBestRequest { public string name; }
    [Serializable] private class MyBestResponse { public bool ok; public int score; }

    // =================================================================================
    // CLIENT SIDE (Diese Methoden ruft dein UI auf)
    // =================================================================================

    /// <summary>
    /// Sendet den Score an den Server zum Speichern.
    /// </summary>
    public void RequestSaveScore(string playerName, int score)
    {
        if (!base.IsClientInitialized) return; // Nur Clients dürfen das senden
        ServerSaveScore(playerName, score);
    }

    /// <summary>
    /// Fragt die globale Bestenliste (Top 10) vom Server an.
    /// </summary>
    public void RequestShowScores()
    {
        if (!base.IsClientInitialized) return;
        ServerGetScores();
    }

    /// <summary>
    /// Fragt nur den persönlichen Highscore eines bestimmten Spielers an.
    /// </summary>
    public void RequestMyBestScore(string playerName)
    {
        if (!base.IsClientInitialized) return;
        ServerGetMyBestScore(playerName);
    }

    // =================================================================================
    // SERVER SIDE (Logik läuft nur auf dem Server/Host)
    // =================================================================================

    // --- 1. SAVE SCORE ---
    [ServerRpc(RequireOwnership = false)]
    private void ServerSaveScore(string playerName, int score)
    {
        Debug.Log($"Server: Speichere Score für '{playerName}' ({score})...");
        StartCoroutine(SaveScoreCoroutine(playerName, score));
    }

    private IEnumerator SaveScoreCoroutine(string playerName, int playerScore)
    {
        var url = $"{baseUrl}/save_score.php";
        var reqObj = new SaveRequest { name = playerName, score = playerScore };
        string json = JsonUtility.ToJson(reqObj);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"DB Save Error: {req.error}");
        }
        else
        {
            var res = JsonUtility.FromJson<SaveResponse>(req.downloadHandler.text);
            if (res != null && res.ok)
                Debug.Log($"Server: Score gespeichert (ID: {res.id})");
            else
                Debug.LogError($"Server PHP Error: {(res != null ? res.error : "Invalid JSON")}");
        }
    }

    // --- 2. GET LIST ---
    [ServerRpc(RequireOwnership = false)]
    private void ServerGetScores(NetworkConnection conn = null)
    {
        Debug.Log($"Server: Lade Highscore-Liste für Client {conn.ClientId}...");
        StartCoroutine(GetScoresCoroutine(conn));
    }

    private IEnumerator GetScoresCoroutine(NetworkConnection targetClient)
    {
        var url = $"{baseUrl}/get_scores.php";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        string[] resultStrings;

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"DB List Error: {req.error}");
            resultStrings = new string[] { "Fehler beim Laden" };
        }
        else
        {
            var res = JsonUtility.FromJson<GetScoreResponse>(req.downloadHandler.text);
            if (res != null && res.ok && res.players != null)
            {
                resultStrings = new string[res.players.Length];
                for (int i = 0; i < res.players.Length; i++)
                {
                    // Formatierung: "Name: 12345"
                    resultStrings[i] = $"{res.players[i].name}: {res.players[i].score}";
                }
            }
            else
            {
                resultStrings = new string[] { "Keine Einträge gefunden" };
            }
        }

        TargetReturnScores(targetClient, resultStrings);
    }

    // --- 3. GET MY BEST ---
    [ServerRpc(RequireOwnership = false)]
    private void ServerGetMyBestScore(string playerName, NetworkConnection conn = null)
    {
        Debug.Log($"Server: Lade persönlichen Rekord für '{playerName}'...");
        StartCoroutine(GetMyBestScoreCoroutine(playerName, conn));
    }

    private IEnumerator GetMyBestScoreCoroutine(string playerName, NetworkConnection targetClient)
    {
        var url = $"{baseUrl}/get_my_best.php";

        // JSON Request
        var reqObj = new MyBestRequest { name = playerName };
        string json = JsonUtility.ToJson(reqObj);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        int finalScore = 0;

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"DB MyBest Error: {req.error}");
        }
        else
        {
            var res = JsonUtility.FromJson<MyBestResponse>(req.downloadHandler.text);
            if (res != null && res.ok)
            {
                finalScore = res.score;
            }
            else
            {
                Debug.LogWarning("Server: Konnte persönlichen Score nicht lesen oder 0.");
            }
        }

        TargetReturnMyBestScore(targetClient, finalScore);
    }

    // =================================================================================
    // BACK TO CLIENT (TargetRpc: Server antwortet dem spezifischen Client)
    // =================================================================================

    [TargetRpc]
    private void TargetReturnScores(NetworkConnection conn, string[] scores)
    {
        // Wir sind auf dem Client!
        // Event auslösen, damit das UI aktualisiert wird
        OnHighscoresReceived?.Invoke(scores);
    }

    [TargetRpc]
    private void TargetReturnMyBestScore(NetworkConnection conn, int score)
    {
        // Wir sind auf dem Client!
        OnMyBestScoreReceived?.Invoke(score);
    }
}