using Fusion;
using UnityEngine;

public class NetworkMatchManager : NetworkBehaviour
{
    public static NetworkMatchManager Instance { get; private set; }
    
    [Networked] public GameModeType SelectedGameMode { get; private set; }
    [Networked] public bool IsMatchStarted { get; private set; }

    [SerializeField] private SceneDataSO sceneData;

    
    public override void Spawned()
    {
        Instance = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }
    
    public void RequestStartMatch(GameModeType requestedGameMode)
    {
        RPC_RequestStartMatch(requestedGameMode);
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestStartMatch(GameModeType requestedGameMode, RpcInfo info = default)
    {
        if (IsMatchStarted)
        {
            Debug.Log("[NetworkMatchManager] Match already started, cannot request new match");
            return;
        }
        
        Debug.Log($"[NetworkMatchManager] Start requested by {info.Source}: {requestedGameMode}");
        
        SelectedGameMode = requestedGameMode;
        IsMatchStarted = true;
        
        if (sceneData == null)
        {
            Debug.LogError("[NetworkMatchManager] Missing SceneData");
            return;
        }
        
        StartMatchOnServer(SelectedGameMode);
    }

    private async void StartMatchOnServer(GameModeType selectedGameMode)
    {
        string sceneName = sceneData.GetSceneName(selectedGameMode);
        await Runner.LoadScene(sceneName);
    }
}
