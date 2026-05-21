using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static LobbyManager Instance { get; private set; }
    
    //inspector refs
    [Header("Network")]
    [SerializeField] private NetworkRunner _networkRunner;
    
    //lobby events
    public event Action OnLobbyJoined;
    public event Action<string> OnLobbyJoinFailed; //string = reason
    public event Action OnLobbyLeave;
    public event Action<List<SessionInfo>> OnSessionListRefreshed;
    
    //room events
    public event Action <SessionInfo> OnRoomCreated;
    public event Action <string> OnRoomCreateFailed;
    public event Action  OnRoomJoined;
    public event Action <string> OnRoomJoinFailed;
    public event Action  OnRoomLeft;
    public event Action <List<PlayerRef>> OnRoomListUpdate;
    
    //lobby state
    public NetworkRunner Runner { get; private set; }
    public bool IsInLobby { get; private set; }
    public bool IsInRoom { get; private set; }
    
    //player and session tracking
    private readonly Dictionary<PlayerRef, NetworkObject> _lobbyPlayers = new();
    private readonly Dictionary<PlayerRef, NetworkObject> _roomPlayers = new();
    private List<SessionInfo> _sessionsList;


    #region Lifecycle
    //singleton
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion
    
    #region PublicAPI
    //join lobby by name, leave empty for default lobby
    public async void JoinLobby(string lobbyID = "")
    {
        if (Runner == null)
        {
            try
            {
                Runner = await CreateRunner();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyManager] Failed to create runner: {e.Message}");
                OnLobbyJoinFailed?.Invoke("Failed to initialize network runner");
                return;
            }
        }

        var result = await Runner.JoinSessionLobby(
            string.IsNullOrEmpty(lobbyID) ? SessionLobby.ClientServer : SessionLobby.Custom,
            string.IsNullOrEmpty(lobbyID) ? null : lobbyID);

        if (result.Ok)
        {
            IsInLobby = true;
            Debug.Log($"[LobbyManager] Joined lobby: '{lobbyID}'");
            OnLobbyJoined?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[LobbyManager] Failed to join lobby: '{lobbyID}'." +
                             $"\nReason: {result.ShutdownReason}");
            OnLobbyJoinFailed?.Invoke(result.ShutdownReason.ToString());
        }
    }
    
    //create new room with custom name and player cap
    public async void CreateRoom(string roomName, int maxPlayers)
    {
        if (Runner == null)
        {
            OnRoomCreateFailed?.Invoke("No network runner available");
            return;
        }

        var args = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            PlayerCount = maxPlayers
        };
        
        var result = await Runner.StartGame(args);

        if (result.Ok)
        {
            IsInRoom = true;
            _roomPlayers.Clear();
            _roomPlayers.Add(Runner.LocalPlayer, null);
            Debug.Log($"[LobbyManager] Created room: '{roomName}'");
            OnRoomCreated?.Invoke(Runner.SessionInfo);
            OnRoomListUpdate?.Invoke(new List<PlayerRef>(_roomPlayers.Keys));
        }
        else
        {
            Debug.LogWarning($"[LobbyManager] Failed to create room: '{roomName}'");
            OnRoomCreateFailed?.Invoke(result.ShutdownReason.ToString());
        }
    }

    public async void JoinRoom(string roomName)
    {
        if (Runner == null)
        {
            OnRoomJoinFailed?.Invoke("Network runner is not initialized");
            return; 
        }

        var args = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = roomName
        };
        
        var result = await Runner.StartGame(args);
        
        if (result.Ok)
        {
            IsInRoom = true;
            Debug.Log($"[LobbyManager] Joined room: '{roomName}'");
            OnRoomJoined?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[LobbyManager] Failed to join room: '{roomName}'");
            OnRoomJoinFailed?.Invoke(result.ShutdownReason.ToString());
        }
    }

    public async void LeaveRoom()
    {
        if (Runner == null || !IsInRoom) return;

        await Runner.Shutdown(false);
        IsInRoom = false;
        _roomPlayers.Clear();
        OnRoomLeft?.Invoke();
        
        JoinLobby();
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
        if (!_roomPlayers.ContainsKey(player))
            _roomPlayers.Add(player, null);
        
        Debug.Log($"[LobbyManager] Player '{player}' joined the room");
        OnRoomListUpdate?.Invoke(new List<PlayerRef>(_roomPlayers.Keys));
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _roomPlayers.Remove(player);
        
        Debug.Log($"[LobbyManager] Player '{player}' left the room");
        OnRoomListUpdate?.Invoke(new List<PlayerRef>(_roomPlayers.Keys));
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        IsInLobby = false;
        IsInRoom = false;
        _roomPlayers.Clear();
        Runner = null;
        Debug.Log($"[LobbyManager] LobbyManager shutdown. Reason: {shutdownReason}");
        OnLobbyLeave?.Invoke();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _sessionsList = sessionList;
        Debug.Log($"[LobbyManager] Session list updated: {sessionList.Count} sessions");
        OnSessionListRefreshed?.Invoke(new List<SessionInfo>(_sessionsList));
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }
    #endregion
    
    #region Helpers

    private async Task<NetworkRunner> CreateRunner()
    {
        //get networkrunner or create one if none exists for whatever reason
        GameObject go;
        if (_networkRunner != null)
            go = Instantiate(_networkRunner.gameObject);
        else
        {
            go = new GameObject("NetworkRunner");
            go.AddComponent<NetworkRunner>();
        }
        
        DontDestroyOnLoad(go);
        var runner = go.GetComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        return runner;
    }
    
    #endregion
}
