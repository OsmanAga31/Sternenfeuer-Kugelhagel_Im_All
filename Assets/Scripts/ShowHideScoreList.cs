using UnityEngine;
using FishNet.Object;

public class ShowHideScoreList : NetworkBehaviour
{
    public static ShowHideScoreList Instance;

    [SerializeField] private GameObject scoreListObjects;

    private void Awake()
    {
        Toggle(false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (IsServerInitialized)
            Instance = this;
    }

    public void Toggle(bool setActive)
    {
        scoreListObjects.SetActive(setActive);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Toggle(true);
    }

}
