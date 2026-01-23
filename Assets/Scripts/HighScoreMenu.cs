using UnityEngine;
using TMPro;

public class HighScoreMenu : MonoBehaviour
{
    public ScoreNetworkManager scoreNetworkManager;

    // UI Elemente
    public TextMeshProUGUI globalHighscoresText; // Die lange Liste
    public TextMeshProUGUI myBestScoreText;      // NEU: Nur mein Score (z.B. oben rechts im Eck)

    // Name des lokalen Spielers (muss irgendwoher kommen, z.B. PlayerPrefs oder Login)
    private string localPlayerName = "Spieler-1";

    private void OnEnable()
    {
        localPlayerName = HubManager.Instance.getName();

        HubManager.Instance.nameInputField.onValueChanged.AddListener(UpdatePlayername); // add listener

        // Beide Events abonnieren
        ScoreNetworkManager.OnHighscoresReceived += UpdateGlobalList;
        ScoreNetworkManager.OnMyBestScoreReceived += UpdateMyBestScore;

        ShowHighscores();

        UpdateAll();
    }

    private void OnDisable()
    {
        HubManager.Instance.nameInputField.onValueChanged.RemoveListener(UpdatePlayername); // remove listener

        // Beide Events abbestellen
        ScoreNetworkManager.OnHighscoresReceived -= UpdateGlobalList;
        ScoreNetworkManager.OnMyBestScoreReceived -= UpdateMyBestScore;
    }


    private void UpdatePlayername(string newName) 
    { 
        localPlayerName = newName;
        ShowHighscores();
    }

    public void ShowHighscores()
    {
        // 1. Globale Liste anfragen
        globalHighscoresText.text = "Lade Liste...";
        scoreNetworkManager.RequestShowScores();

        // 2. Meinen persönlichen Bestwert anfragen
        myBestScoreText.text = "Lade...";

        // Hier musst du den echten Namen des Spielers übergeben!
        // Wenn du den Namen im Spiel noch nicht gespeichert hast, musst du das noch tun.
        // Beispiel: localPlayerName = PlayerPrefs.GetString("PlayerName", "Guest");
        scoreNetworkManager.RequestMyBestScore(localPlayerName);
    }

    // Bestehende Methode für die Liste
    private void UpdateGlobalList(string[] entries)
    {
        string tmp = "Scoreboard:\n";
        globalHighscoresText.text = "";
        foreach (string entry in entries)
        {
            tmp += entry + "\n";
        }
        globalHighscoresText.text = tmp;
    }

    // NEU: Methode für den persönlichen Score
    private void UpdateMyBestScore(int score)
    {
        myBestScoreText.text = $"Mein Rekord: {score}";
    }


    // This function is not from the AI.
    private void UpdateAll()
    {
        if (ScoreNetworkManager.Instance == null) return;

        ScoreNetworkManager.Instance.RequestShowScores();
        ScoreNetworkManager.Instance.RequestMyBestScore(localPlayerName);
    }

}