using System;
using System.Collections;
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
    [SerializeField] private bool isServer = false;
    #endregion
    
    #region Events

    public event Action ServerStarted;
    public event Action LocalManagersReady;
    public event Action<string> ServerStartFailed;
    public event Action ClientStarted;
    public event Action<string> ClientStartFailed;
    
    #endregion
    
    #region Unity Methods

    private void Start()
    {
        StartCoroutine(WaitForLocalManagers());
        
        if (isServer)
        {
            StartServer();
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
            string errorMessage = $"[NetworkStartupManager] Failed to start server: {result.ShutdownReason}";
            ServerStartFailed?.Invoke(errorMessage);
            Debug.LogError(errorMessage);
            return;
        }

        StartCoroutine(WaitForServerManagers());
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

            if (result.ShutdownReason == ShutdownReason.GameClosed)
            {
                ClientStartFailed?.Invoke("Game is already in session");
                return;
            }
            
            ClientStartFailed?.Invoke(result.ShutdownReason.ToString());
            return;
        }

        StartCoroutine(WaitForClientNetworkManagers());
    }
    
    #endregion
    
    #region Helpers

    private IEnumerator WaitForLocalManagers()
    {
        while (ServerLobbyManager.Instance == null ||
               NetworkMatchManager.Instance == null)
        {
            yield return null;
        }

        LocalManagersReady?.Invoke();
    }
    
    private IEnumerator WaitForServerManagers()
    {
        while (!AreNetworkManagersReady())
        {
            Debug.Log("[NetworkStartupManager] Waiting for network managers to be ready...");
            yield return null;
        }
        
        Debug.Log("[NetworkStartupManager] Server started successfully");
        ServerStarted?.Invoke();
    }

    private IEnumerator WaitForClientNetworkManagers()
    {
        while (ServerLobbyManager.Instance == null ||
               NetworkMatchManager.Instance == null ||
               ServerLobbyManager.Instance.Runner == null ||
               NetworkMatchManager.Instance.Runner == null)
        {
            Debug.Log("[NetworkStartupManager] Client waiting for network managers to be ready...");
            yield return null;
        }

        ClientStarted?.Invoke();
    }

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

    private bool AreNetworkManagersReady()
    {
        return ServerLobbyManager.Instance != null && NetworkMatchManager.Instance != null;
    }
    
    #endregion
}
