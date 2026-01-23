using UnityEngine;
using FishNet.Object;
using UnityEngine.SceneManagement;
using FishNet.Managing.Scened;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Events;

public class HubManager : NetworkBehaviour
{
    public static HubManager Instance;

    [SerializeField] private Button startButton;
    public TMP_InputField nameInputField;
    private TMP_Text buttonText;
    private bool gameIsRunning = false;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {

        if (!IsServerInitialized) 
        {
            startButton.gameObject.SetActive(false);
        }
        else
        {
            buttonText = startButton.GetComponentInChildren<TMP_Text>();
            startButton.onClick.AddListener(StartGameButton);
        }

    }

    [Server]
    public void StartGameButton()
    {
        if (EnemySpawner.Instance == null)
        {
            Debug.LogError("EnemySpawner instance not found!");
            return;
        }

        if (!gameIsRunning)
        {
            EnemySpawner.Instance.StartGame();
            buttonText.text = "Stop Game";
            gameIsRunning = true;
        }
        else
        {
            EnemySpawner.Instance.StopGame();
            buttonText.text = "Start Game";
            gameIsRunning = false;
        }
        ShowHideScoreList.Instance.ToggleForAll(!gameIsRunning);
        ToggleNameField(!gameIsRunning);
        // reset alive status for all players

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            int playerId = player.GetComponent<PlayerController>().PlayerOwnerId;
            ScoreManager.Instance.playerAliveStatus[playerId] = true;
        }


    }

    public string getName()
    {
        return nameInputField.text;
    }

    [ObserversRpc]
    private void ToggleNameField(bool actv)
    {
        nameInputField.gameObject.SetActive(actv);
    }

}
