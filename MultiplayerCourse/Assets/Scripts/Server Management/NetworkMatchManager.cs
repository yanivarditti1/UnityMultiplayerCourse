using System;
using Fusion;
using UnityEngine;

public class NetworkMatchManager : NetworkBehaviour
{
    #region Fields
    //singleton
    public static NetworkMatchManager Instance { get; private set; }
    
    //networked values
    [Networked] public GameModeType SelectedGameMode { get; private set; }
    [Networked] public bool IsMatchStarted { get; private set; }
    [Networked] public ServerMatchState MatchState { get; private set; }
    
    
    [Header("References")]
    [SerializeField] private ServerLobbyManager serverLobbyManager;
    [SerializeField] private SceneDataSO sceneData;
    
    #endregion
    
    #region Lifecycle
    
    public override void Spawned()
    {
        Instance = this;

        if (Object.HasStateAuthority)
        {
            MatchState = ServerMatchState.WaitingForPlayers;
            SelectedGameMode = GameModeType.FreeForAll;
            IsMatchStarted = false;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }
    
    #endregion
    
    #region Public API

    public void RequestGameMode(GameModeType requestedGameMode)
    {
        RPC_RequestGameMode(requestedGameMode);
    }
    
    public void RequestStartMatch()
    {
        RPC_RequestStartMatch();
    }
    
    #endregion
    
    #region RPC
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestStartMatch(RpcInfo info = default)
    {
        if (MatchState != ServerMatchState.WaitingForPlayers)
            return;
        
        if (IsMatchStarted)
        {
            Debug.Log("[NetworkMatchManager] Match already started, cannot request new match");
            return;
        }
        
        if (sceneData == null)
        {
            Debug.LogError("[NetworkMatchManager] Missing SceneData");
            return;
        }

        if (!serverLobbyManager.IsLobbyLeader(info.Source))
            return;

        if (serverLobbyManager == null || !serverLobbyManager.CanStartMatch())
            return;
        
        MatchState = ServerMatchState.Starting;
        IsMatchStarted = true;

        if (Runner.SessionInfo != null)
        {
            Runner.SessionInfo.IsOpen = false;
            Runner.SessionInfo.IsVisible = false;
        }
        
        StartMatchOnServer(SelectedGameMode);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestGameMode(GameModeType requestedGameMode, RpcInfo info = default)
    {
        if (MatchState != ServerMatchState.WaitingForPlayers)
            return;

        if (serverLobbyManager == null || !serverLobbyManager.IsLobbyLeader(info.Source))
        {
            Debug.Log($"[NetworkMatchManager] {info.Source} tried to change mode but is not lobby leader");
            return;
        }
        
        if (serverLobbyManager.ReadyPlayerCount > 0)
        {
            Debug.Log("[NetworkMatchManager] Cannot change game mode while players are ready");
            return;
        }
        
        SelectedGameMode = requestedGameMode;
    }
    
    #endregion
    
    #region Helpers

    private async void StartMatchOnServer(GameModeType selectedGameMode)
    {
        string sceneName = sceneData.GetSceneName(selectedGameMode);
        await Runner.LoadScene(sceneName);
        
        MatchState = ServerMatchState.InProgress;
    }
    
    #endregion
}


public enum ServerMatchState
{
    WaitingForPlayers,
    Starting,
    InProgress,
    Finished
}