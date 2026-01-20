using UnityEngine;
using TMPro;
//using FishNet.Connection;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    private void Start()
    {
        if(scoreText == null)
            scoreText = transform.parent.GetComponentInChildren<TextMeshProUGUI>();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
    }

    private void Update()
    {
        UpdateScore(ScoreManager.Instance?.BaseScore ?? 0);
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void OnDestroy()
    {
        if(ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
    }
}
