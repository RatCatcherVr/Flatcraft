using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : NetworkManager
{
    public List<string> prefabDirectories = new List<string>();
    public GameObject WorldManagerPrefab;
    internal bool _initialized;

    public override void Awake()
    {
        base.Awake();
        _initialized = true;
    }

    private static MultiplayerManager CreateMultiplayerManager()
    {
        return Instantiate(Resources.Load<GameObject>("Prefabs/Multiplayer Manager"))
            .GetComponent<MultiplayerManager>();
    }

    public static async void HostGameAsync()
    {
        UnityEngine.AsyncOperation sceneLoad = SceneManager.LoadSceneAsync("Game");
        while (!sceneLoad.isDone)
            await Task.Delay(10);

        MultiplayerManager multiplayerManager = CreateMultiplayerManager();
        while (!multiplayerManager._initialized)
            await Task.Delay(10);

        multiplayerManager.StartHost();
    }

    public static async void JoinGameAsync(string address)
    {
        UnityEngine.AsyncOperation sceneLoad = SceneManager.LoadSceneAsync("Game");
        while (!sceneLoad.isDone)
            await Task.Delay(10);

        MultiplayerManager multiplayerManager = CreateMultiplayerManager();
        while (!multiplayerManager._initialized)
            await Task.Delay(10);

        multiplayerManager.networkAddress = address;
        multiplayerManager.StartClient();
    }

    public void StopConnection()
    {
        switch (singleton.mode)
        {
            case NetworkManagerMode.ClientOnly:
                singleton.StopClient();
                break;
            case NetworkManagerMode.Host:
                singleton.StopHost();
                break;
        }

        SceneManager.LoadScene("MainMenu");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GameObject worldManager = Instantiate(WorldManagerPrefab);
        NetworkServer.Spawn(worldManager);
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        if (newSceneName == onlineScene)
            NetworkClient.Ready();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        if (SceneManager.GetActiveScene().name == "Game")
            SceneManager.LoadScene("MultiplayerDisconnectedMenu");

        Destroy(gameObject);
    }
}