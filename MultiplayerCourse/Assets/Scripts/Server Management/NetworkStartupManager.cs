using System;
using UnityEngine;
using System.Threading.Tasks;
using Fusion;
using UnityEngine.SceneManagement;

public class NetworkStartupManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Network Runner")]
    //[SerializeField] private NetworkRunner networkRunnerPrefab;
    [SerializeField] private NetworkRunner runner;
    
    //configurable
    [Header("Configuration")]
    [SerializeField] private string sessionName = "Yamit 2000 Test Room";
    [SerializeField] private string gameplaySceneName = "ConquestScene";
    [SerializeField] private int maxPlayers = 10;
    
    //debugging
    [Header("Debug")]
    [SerializeField] private bool autoStartServer;
    
    //this is true by default for debugging in multiplayer play mode and must be turned off from the server user
    [SerializeField] private bool autoStartClient = true;
    #endregion
    
    #region Events

    public event Action ClientStarted;
    public event Action<string> ClientStartFailed;
    
    #endregion
    
    #region Unity Methods

    private void Start()
    {
        if (autoStartServer)
        {
            StartServer();
        }
        else if (autoStartClient)
        {
            StartClient();
        }
    }
    
    #endregion
    
    #region Startup Methods
    
    public async void StartServer()
    {
        Debug.Log("[NetworkStartupManager] Starting server...");
        await StartServerAsync();
    }

    public async void StartClient()
    {
        Debug.Log("[NetworkStartupManager] Starting client...");
        await StartClientAsync();
    }

    private async Task StartServerAsync()
    {
        NetworkRunner activeRunner = GetOrCreateRunner();

        activeRunner.ProvideInput = false;

        StartGameArgs startGameArgs = new StartGameArgs
        {
            GameMode = GameMode.Server,
            SessionName = sessionName,
            PlayerCount = maxPlayers,
            Scene = GetCurrentSceneInfo(),
            SceneManager = VerifySceneManager(activeRunner)
        };
        
        StartGameResult result = await activeRunner.StartGame(startGameArgs);

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkStartupManager] Failed to start server: {result.ShutdownReason}");
            return;
        }
    }
    
    private async Task StartClientAsync()
    {
        NetworkRunner activeRunner = GetOrCreateRunner();
        
        activeRunner.ProvideInput = true;
        
        StartGameArgs startGameArgs = new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            Scene = GetCurrentSceneInfo(),
            SceneManager = VerifySceneManager(activeRunner)
        };
        
        StartGameResult result = await activeRunner.StartGame(startGameArgs);

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkStartupManager] Failed to start client: {result.ShutdownReason}");
            ClientStartFailed?.Invoke(result.ShutdownReason.ToString());
            return;
        }
        
        ClientStarted?.Invoke();
    }
    
    #endregion
    
    #region Helpers

    private NetworkRunner GetOrCreateRunner()
    {
        if (runner != null)
            return runner;
        
        GameObject runnerObject = new GameObject("Network Runner");
        runner = runnerObject.AddComponent<NetworkRunner>();
        
        DontDestroyOnLoad(runnerObject);
        
        return runner;
    }

    private NetworkSceneManagerDefault VerifySceneManager(NetworkRunner activeRunner)
    {
        NetworkSceneManagerDefault sceneManager = activeRunner.GetComponent<NetworkSceneManagerDefault>();
        
        if (sceneManager == null)
            sceneManager = activeRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        
        return sceneManager;   
    }

    private NetworkSceneInfo GetCurrentSceneInfo()
    {
        SceneRef sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single);
        
        return sceneInfo;
    }
    
    #endregion
}
