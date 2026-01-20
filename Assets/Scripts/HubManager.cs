using UnityEngine;
using FishNet.Object;
using UnityEngine.SceneManagement;
using FishNet.Managing.Scened;

public class HubManager : NetworkBehaviour
{
    [ObserversRpc]
    public void ChangeScene()
    {
        SceneLoadData sld = new SceneLoadData("GameScene");
        base.SceneManager.LoadGlobalScenes(sld);
    }
}
