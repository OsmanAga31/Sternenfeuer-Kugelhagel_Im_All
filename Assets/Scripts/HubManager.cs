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
    //[SerializeField] private TMP_InputField nameInputField;
    private TMP_Text buttonText;
    private bool gameIsRunning = false;

    public UnityEvent<string> OnInputfieldChangeValue;

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
            startButton.onClick.AddListener(StartGame);
        }

    }

    //public void SubscribeToValueChange()
    //{
    //    nameInputField.onValueChanged.AddListener(UpdateName);
    //}

    [Server]
    public void StartGame()
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
    }

    //public string GetPlayerName()
    //{
    //    return nameInputField.text;
    //}

    //[ServerRpc]
    //public void UpdateName(string val) 
    //{ 

    //    OnInputfieldChangeValue?.Invoke(val);
    //}

}
