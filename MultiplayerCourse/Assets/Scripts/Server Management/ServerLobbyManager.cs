using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class ServerLobbyManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    //singleton
    public static ServerLobbyManager Instance { get; private set; }
    
    //networked fields
    [Networked, Capacity(10)]
    private NetworkDictionary<PlayerRef, LobbyPlayerState> Players => default;
    [Networked] public int ConnectedPlayerCount { get; private set;}
    [Networked] public int ReadyPlayerCount { get; private set;}
    [Networked] public PlayerRef LobbyLeader { get; private set;}
    
    //events
    public event Action<PlayerRef, string> NicknameAccepted;
    public event Action<PlayerRef, string> NicknameRejected;
    public event Action<PlayerRef, string> NicknameChanged;
    public event Action<PlayerRef, bool> ReadyStateChanged;
    public event Action<PlayerRef> LobbyLeaderChanged;
    public event Action LobbyStateChanged;
    public event Action<PlayerRef> PlayerJoinedLobby;
    public event Action<PlayerRef> PlayerLeftLobby;
    
    #region Lifecycle

    private void Awake()
    {
        Instance = this;
    }
    
    public override void Spawned()
    {
        Instance = this;
        Runner.AddCallbacks(this);

        if (Object.HasStateAuthority)
        {
            LobbyLeader = PlayerRef.None;
            ConnectedPlayerCount = 0;
            ReadyPlayerCount = 0;
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Runner.RemoveCallbacks(this);
        
        if (Instance == this)
            Instance = null;
    }
    
    #endregion
    
    #region PublicAPI

    public IEnumerable<KeyValuePair<PlayerRef, LobbyPlayerState>> GetPlayers()
    {
        foreach (var player in Players)
            yield return player;
    }

    public bool TryGetPlayerState(PlayerRef player, out LobbyPlayerState playerState)
    {
        return Players.TryGet(player, out playerState);
    }

    public bool IsLocalPlayer(PlayerRef player)
    {
        return Runner != null && Runner.LocalPlayer == player;
    }

    public void RequestNickname(string nickname)
    {
        RPC_RequestNickname(nickname);
    }

    public void RequestReady(bool newReadyState)
    {
        RPC_RequestReady(newReadyState);
    }

    public bool CanStartMatch()
    {
        if (ConnectedPlayerCount < 2)
            return false;

        if (ReadyPlayerCount < 2)
            return false;
        
        return ReadyPlayerCount == ConnectedPlayerCount;
    }

    public bool IsNicknameAvailable(string nickname, PlayerRef requestingPlayer)
    {
        foreach (var entry in Players)
        {
            if (entry.Key == requestingPlayer)
                continue;

            if (!entry.Value.HasNickname)
                continue;

            var existingNickname = entry.Value.Nickname.ToString();
            if (existingNickname.Equals(nickname, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        
        return true;   
    }

    public bool IsLobbyLeader(PlayerRef player)
    {
        return LobbyLeader != PlayerRef.None && LobbyLeader == player;
    }

    public bool IsLocalPlayerLobbyLeader()
    {
        return Runner != null && IsLobbyLeader(Runner.LocalPlayer);
    }
    
    #endregion
    
    #region RPCs

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayerJoinedLobby(PlayerRef player)
    {
        PlayerJoinedLobby?.Invoke(player);
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayerLeftLobby(PlayerRef player)
    {
        PlayerLeftLobby?.Invoke(player);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestNickname(string nickname, RpcInfo info = default)
    {
        PlayerRef player = info.Source;

        if (!Players.TryGet(player, out LobbyPlayerState playerState))
        {
            Debug.Log($"[ServerLobbyManager] Player {player} requested nickname but is not in lobby");
            return;
        }
        
        nickname = nickname.Trim();
        
        Debug.Log($"[ServerLobbyManager] Player {player} requested nickname: {nickname}");
        
        if (string.IsNullOrEmpty(nickname))
        {
            RPC_NicknameRejected(player, "Nickname cannot be empty");
            return;
        }

        if (nickname.Length > 16)
        {
            RPC_NicknameRejected(player, "Nickname cannot be longer than 16 characters");
            return;
        }

        if (!IsNicknameAvailable(nickname, player))
        {
            RPC_NicknameRejected(player, "Nickname already in use");
            return;
        }
        
        playerState.Nickname = nickname;
        playerState.HasNickname = true;
        
        Players.Set(player, playerState);
        RecalculateCounts();
        
        RPC_NicknameAccepted(player, nickname);
        RPC_NicknameChanged(player, nickname);
        RPC_LobbyStateChanged();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NicknameAccepted([RpcTarget] PlayerRef player, string nickname)
    {
        NicknameAccepted?.Invoke(player, nickname);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NicknameRejected([RpcTarget] PlayerRef player, string reason)
    {
        NicknameRejected?.Invoke(player, reason);
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NicknameChanged([RpcTarget] PlayerRef player, string nickname)
    {
        NicknameChanged?.Invoke(player, nickname);
    }   

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestReady(bool newReadyState, RpcInfo info = default)
    {
        PlayerRef player = info.Source;

        if (!Players.TryGet(player, out LobbyPlayerState playerState))
        {
            Debug.Log($"[ServerLobbyManager] Player {player} requested ready status but is not in lobby");
            return;
        }

        if (!playerState.HasNickname)
        {
            RPC_NicknameRejected(player, "You must set a nickname before you can request ready status");
            return;
        }
        
        playerState.IsReady = newReadyState;
        
        Players.Set(player, playerState);
        RecalculateCounts();

        RPC_ReadyStateChanged(player, newReadyState);
        RPC_LobbyStateChanged();
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReadyStateChanged(PlayerRef player, bool newReadyState)
    {
        ReadyStateChanged?.Invoke(player, newReadyState);
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_LobbyStateChanged()
    {
        LobbyStateChanged?.Invoke();
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_LobbyLeaderChanged(PlayerRef newLeader)
    {
        LobbyLeaderChanged?.Invoke(newLeader);
    }
    
    #endregion
    
    #region INetworkRunnerCallbacks
    
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;

        if (NetworkMatchManager.Instance != null &&
            NetworkMatchManager.Instance.MatchState != ServerMatchState.WaitingForPlayers)
        {
            runner.Disconnect(player);
            return;
        }
        
        Players.Set(player, new LobbyPlayerState());
        RPC_PlayerJoinedLobby(player);

        if (LobbyLeader == PlayerRef.None)
        {
            LobbyLeader = player;
            Debug.Log($"[ServerLobbyManager] Lobby leader assigned to {player}");
            
            RPC_LobbyLeaderChanged(LobbyLeader);
            RPC_LobbyStateChanged();
        }
        RecalculateCounts();
        RPC_LobbyStateChanged();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;

        Players.Remove(player);
        RPC_PlayerLeftLobby(player);

        if (LobbyLeader == player)
            AssignNewLobbyLeader();
        
        RecalculateCounts();
        RPC_LobbyStateChanged();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        //throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        //throw new NotImplementedException();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        //throw new NotImplementedException();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        //throw new NotImplementedException();
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        //throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        //throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        //throw new NotImplementedException();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        //throw new NotImplementedException();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        //throw new NotImplementedException();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        //throw new NotImplementedException();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        //throw new NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        //throw new NotImplementedException();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        //throw new NotImplementedException();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        //throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        //throw new NotImplementedException();
    }
    
    #endregion
    
    #region Helpers

    private void RecalculateCounts()
    {
        ConnectedPlayerCount = 0;
        ReadyPlayerCount = 0;
        
        foreach (var player in Players)
        {
                ConnectedPlayerCount++;
            
            if (player.Value.IsReady)
                ReadyPlayerCount++;
        }
    }

    private void AssignNewLobbyLeader()
    {
        LobbyLeader = PlayerRef.None;
        
        foreach (var player in Players)
        {
            LobbyLeader = player.Key;
            Debug.Log($"[ServerLobbyManager] New lobby leader assigned: {LobbyLeader}");
            
            RPC_LobbyLeaderChanged(LobbyLeader);
            RPC_LobbyStateChanged();
            return;
        }
        
        Debug.Log("[ServerLobbyManager] No lobby leader assigned, lobby is empty");
    }
    
    #endregion
}