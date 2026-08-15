using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerUiManager : MonoBehaviour
{
    #region References
    
    [Header("Panels")] 
    [SerializeField] private GameObject connectPanel;
    [SerializeField] private GameObject preMatchPanel;
    
    [Header("Connection")]
    [SerializeField] private Button joinServerButton;
    [SerializeField] private TextMeshProUGUI connectStatusText;
    [SerializeField] private float timeoutWaitTime = 5f;
    
    [Header("Pre-Match")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button confirmGameModeButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private TextMeshProUGUI preMatchStatusText;
    
    [Header("References")]
    [SerializeField] private NetworkStartupManager networkStartupManager;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private ServerLobbyPlayerUiEntry playerEntryPrefab;
    
    private readonly Dictionary<PlayerRef, ServerLobbyPlayerUiEntry> _playerEntries = new();

    private bool _subscribedToServerManagers = false;
    
    #endregion

    private bool _pendingReadyAfterNicknameChanged;
    
    #region Unity Lifecycle

    private void Start()
    {
        ShowConnectPanel();
        SetConnectedStatus("Waiting for server managers...");
        joinServerButton.interactable = false;
        
        SetUpButtonListeners();

        if (networkStartupManager != null)
            SubscribeToNetworkStartupEvents();
        else
            Debug.LogError("[ServerUiManager] NetworkStartupManager is null");
        
        SetupGameModeDropdown();
        RefreshConnectPanelStatus();
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
        
        if (networkStartupManager != null)
        {
            UnsubscribeToNetworkStartupEvents();
        }
        
        UnsubscribeToServerManagers();
    }
    
    #endregion
    
    #region Handlers

    private void HandleLocalManagersReady()
    {
        SubscribeToServerManagers();
        
        bool isServer = networkStartupManager != null &&
                        networkStartupManager.IsLocalPlayerServer();

        if (isServer)
        {        
            joinServerButton.interactable = false;
            SetConnectedStatus("Server Online");
            return;
        }
        
        SetConnectedStatus("Server Online");
        StartCoroutine(ShowPanelOnPlayerJoined());
    }

    private void HandleServerStarted()
    {
        bool isServer = networkStartupManager != null && networkStartupManager.IsLocalPlayerServer();
        joinServerButton.interactable = !isServer;
        SetConnectedStatus("Server online");
    }
    
    private void HandleServerStartFailed(string reason)
    {
        bool isServer = networkStartupManager != null && networkStartupManager.IsLocalPlayerServer();
        joinServerButton.interactable = !isServer;
        SetConnectedStatus($"Failed to start server: {reason}");
    }
    
    private void HandleJoinServerClicked()
    {
        if (networkStartupManager == null)
        {
            SetConnectedStatus("NetworkStartupManager is null");
            return;
        }
        
        joinServerButton.interactable = false;
        SetConnectedStatus("Connecting to server...");

        if (ServerLobbyManager.Instance != null && ServerLobbyManager.Instance.Runner != null)
        {
            ServerLobbyManager.Instance.RequestJoinLobby();
        }
        else
        {
            networkStartupManager.StartClient();
        }
    }

    private void HandleJoinLobbyAccepted()
    {
        ShowPreMatchPanel();
        RefreshPlayerList();
        RefreshPreMatchControls();
        SetConnectedStatus("Joined Lobby");
        SetPreMatchStatus("Waiting ready...");
    }
    private void HandleJoinLobbyRejected(string reason)
    {
        ShowConnectPanel();
        SetConnectedStatus($"Failed to join lobby: {reason}");
    }
    
    private void HandleConfirmGameModeClicked()
    {
        if (!ManagersReady())
            return;

        if (!ServerLobbyManager.Instance.IsLocalPlayerLobbyLeader())
        {
            SetPreMatchStatus("Only lobby leader can confirm game mode");
            return;
        }
        
        GameModeType requestedGameMode = GetSelectedGameMode();
        NetworkMatchManager.Instance.RequestGameMode(requestedGameMode);
        SetPreMatchStatus($"Set game mode: {requestedGameMode}");

        confirmGameModeButton.interactable = false;
        gameModeDropdown.interactable = false;
    }

    private void HandleReadyClicked()
    {
        if (!ManagersReady())
            return;
        
        //validate nickname
        var requestedNickname = nicknameInputField.text.Trim();
        if (!ValidateNickname(requestedNickname))
            return;

        _pendingReadyAfterNicknameChanged = true;
        
        Debug.Log($"[ServerUiManager] Requesting validated nickname: {requestedNickname}");
        
        SetPreMatchStatus($"Pending nickname validation...");
        
        ServerLobbyManager.Instance.RequestNickname(requestedNickname);
    }
    
    private void HandleStartMatchClicked()
    {
        if (!ManagersReady())
            return;

        if (!ServerLobbyManager.Instance.IsLocalPlayerLobbyLeader())
        {
            SetPreMatchStatus("Only lobby leader can start match");
            return;
        }

        if (!ServerLobbyManager.Instance.CanStartMatch())
        {
            SetPreMatchStatus("Cannot start match while players are not ready");
            return;       
        }
        
        startMatchButton.interactable = false;
        SetPreMatchStatus("Starting match...");
        
        NetworkMatchManager.Instance.RequestStartMatch();
    }

    private void HandleClientStarted()
    {
        ShowPreMatchPanel();
        SetConnectedStatus("Connected to server");
        SubscribeToServerManagers();
        
        RefreshPreMatchControls();
    }

    private void HandleClientStartFailed(string reason)
    {
        if (networkStartupManager != null && networkStartupManager.IsLocalPlayerServer())
        {
            SetConnectedStatus("Server online");
            return;
        }
        
        joinServerButton.interactable = true;
        SetConnectedStatus($"Failed to connect to server: {reason}");
    }

    private void HandleLobbyStateChanged()
    {
        RefreshPreMatchControls();
    }

    private void HandleLobbyLeaderChanged(PlayerRef newLeader)
    {
        RefreshPreMatchControls();
        RefreshPlayerList();
    }

    private void HandleNicknameAccepted(PlayerRef player, string nickname)
    {
        SetPreMatchStatus($"Nickname set: {nickname}\nReduesting ready...");
        
        nicknameInputField.interactable = false;

        if (ServerLobbyManager.Instance != null && ServerLobbyManager.Instance.Runner.LocalPlayer == player)
        {
            PlayerDataPersistanceManager.Instance.SetNickname(nickname);
        }

        if (!_pendingReadyAfterNicknameChanged)
        {
            Debug.Log("[ServerUiManager] Nickname accepted without ready request");
            return;
        }
        
        StartCoroutine(RequestReadyNextFrame());
    }

    private void HandleNicknameRejected(PlayerRef player, string reason)
    {
        _pendingReadyAfterNicknameChanged = false;
        SetPreMatchStatus(reason);
        readyButton.interactable = true;
    }

    private void HandleReadyStateChanged(PlayerRef player, bool newReadyState)
    {
        if (ServerLobbyManager.Instance != null && ServerLobbyManager.Instance.Runner.LocalPlayer == player)
        {
            //isReadyToggle.isOn = newReadyState;
            SetPreMatchStatus(newReadyState ? "Ready" : "Not Ready");
            _pendingReadyAfterNicknameChanged = false;
            readyButton.interactable = !newReadyState;
            leaveLobbyButton.interactable = !newReadyState;
        }

        if (_playerEntries.TryGetValue(player, out var entry))
        {
            entry.SetReady(newReadyState);
        }
        else
        {
            Debug.LogError($"[ServerUiManager] Player {player} not found in player list");
        }

        if (ServerLobbyManager.Instance.IsLocalPlayerLobbyLeader() && ServerLobbyManager.Instance.CanStartMatch())
        {
            SetPreMatchStatus("Ready to start match");
        }
        if (ServerLobbyManager.Instance.IsLocalPlayerLobbyLeader() && !ServerLobbyManager.Instance.CanStartMatch())
        {
            SetPreMatchStatus("Waiting for players to be ready...");
        }
        if (!ServerLobbyManager.Instance.IsLocalPlayerLobbyLeader())
        {
            SetPreMatchStatus("Waiting for lobby leader to start match");
        }
        
        RefreshPreMatchControls();
        RefreshPlayerList();
    }

    private void HandlePlayerJoinedLobby(PlayerRef player)
    {
        RefreshPlayerList();
    }

    private void HandleLeaveLobbyClicked()
    {
        if (!ManagersReady())
            return;
        
        leaveLobbyButton.interactable = false;
        SetPreMatchStatus("Leaving lobby...");
        
        ServerLobbyManager.Instance.RequestLeaveLobby();
    }

    private void HandleLeaveLobbyAccepted()
    {
        ClearPlayerList();

        nicknameInputField.interactable = true;
        readyButton.interactable = true;
        leaveLobbyButton.interactable = true;
        
        ShowConnectPanel();
        SetPreMatchStatus("Leaving lobby...");
        SetConnectedStatus("Left Lobby");
    }
    
    /*private void HandleClientStopped()
    {
        nicknameInputField.interactable = true;
        readyButton.interactable = true;
        leaveLobbyButton.interactable = true;
        
        ShowConnectPanel();
        SetConnectedStatus("Left Lobby");
    }*/

    private void HandleLeaveLobbyRejected(string reason)
    {
        SetPreMatchStatus(reason);
        leaveLobbyButton.interactable = true;
        
        RefreshPreMatchControls();
    }

    private void HandlePlayerLeftLobby(PlayerRef player)
    {
        if (_playerEntries.TryGetValue(player, out var entry))
        {
            Destroy(entry.gameObject);
            _playerEntries.Remove(player);
        }

        RefreshPlayerList();
    }

    private void HandleNicknameChanged(PlayerRef player, string nickname)
    {
        RefreshPlayerList();
    }
    
    #endregion
    
    #region Helpers

    private void SubscribeToNetworkStartupEvents()
    {
        networkStartupManager.LocalManagersReady += HandleLocalManagersReady;
        networkStartupManager.ServerStarted += HandleServerStarted;
        networkStartupManager.ServerStartFailed += HandleServerStartFailed;
        networkStartupManager.ClientStarted += HandleClientStarted;
        networkStartupManager.ClientStartFailed += HandleClientStartFailed;
        //networkStartupManager.ClientStopped += HandleClientStopped;
        
        Debug.Log("[ServerUiManager] Client subscribed to NetworkStartupManager events");
    }

    private void UnsubscribeToNetworkStartupEvents()
    {
        networkStartupManager.ServerStarted -= HandleServerStarted;
        networkStartupManager.ServerStartFailed -= HandleServerStartFailed;
        networkStartupManager.ClientStarted -= HandleClientStarted;
        networkStartupManager.ClientStartFailed -= HandleClientStartFailed;
        networkStartupManager.LocalManagersReady -= HandleLocalManagersReady;
        //networkStartupManager.ClientStopped -= HandleClientStopped;
    }
    
    private void SetUpButtonListeners()
    {
        joinServerButton.onClick.AddListener(HandleJoinServerClicked);
        readyButton.onClick.AddListener(HandleReadyClicked);
        startMatchButton.onClick.AddListener(HandleStartMatchClicked);
        confirmGameModeButton.onClick.AddListener(HandleConfirmGameModeClicked);
        leaveLobbyButton.onClick.AddListener(HandleLeaveLobbyClicked);
    }

    private void RemoveButtonListeners()
    {
        if (joinServerButton != null)
            joinServerButton.onClick.RemoveListener(HandleJoinServerClicked);
        
        if (startMatchButton != null)
            startMatchButton.onClick.RemoveListener(HandleStartMatchClicked);
        
        if (readyButton != null)
            readyButton.onClick.RemoveListener(HandleReadyClicked);
        
        if (confirmGameModeButton != null)
            confirmGameModeButton.onClick.RemoveListener(HandleConfirmGameModeClicked);
        
        if (leaveLobbyButton != null)
            leaveLobbyButton.onClick.RemoveListener(HandleLeaveLobbyClicked);
    }
    
    private IEnumerator ShowPanelOnPlayerJoined()
    {
        while (ServerLobbyManager.Instance == null || ServerLobbyManager.Instance.Runner == null)
            yield return null;

        PlayerRef localPlayer = ServerLobbyManager.Instance.Runner.LocalPlayer;

        float timeout = Time.time + timeoutWaitTime;

        while (Time.time < timeout)
        {
            if (ServerLobbyManager.Instance.IsPlayerInLobby(localPlayer))
            {
                ShowPreMatchPanel();
                RefreshPlayerList();
                RefreshPreMatchControls();
                SetConnectedStatus("Joined Lobby");
                SetPreMatchStatus("Returned To Lobby");
                yield break;
            }
            
            yield return null;
        }
        
        ShowConnectPanel();
        SetConnectedStatus("Failed to join lobby");
    }
    
    private bool LocalPlayerAlreadyInLobby()
    {
        if (ServerLobbyManager.Instance == null)
            return false;
        
        if (ServerLobbyManager.Instance.Runner == null)
            return false;

        return ServerLobbyManager.Instance.IsPlayerInLobby(ServerLobbyManager.Instance.Runner.LocalPlayer);
    }

    private IEnumerator RequestReadyNextFrame()
    {
        yield return null;
        
        if (!ManagersReady())
            yield break;
        
        Debug.Log("[ServerUiManager] Waited 1 frame, requesting ready after nickname change");
        
        _pendingReadyAfterNicknameChanged = false;
        ServerLobbyManager.Instance.RequestReady(true);
    }

    private bool ManagersReady()
    {
        if (ServerLobbyManager.Instance == null)
        {
            SetConnectedStatus("ServerLobbyManager not ready yet");
            SetPreMatchStatus("ServerLobbyManager not ready yet");
            return false;
        }

        if (NetworkMatchManager.Instance == null)
        {
            SetConnectedStatus("NetworkMatchManager not ready yet");
            SetPreMatchStatus("NetworkMatchManager not ready yet");
            return false;
        }

        return true;
    }

    private void RefreshPlayerList()
    {
        if (!ManagersReady())
            return;

        foreach (var entry in _playerEntries.Values)
        {
            Destroy(entry.gameObject);
        }

        _playerEntries.Clear();

        foreach (var playerEntry in ServerLobbyManager.Instance.GetPlayers())
        {
            PlayerRef playerRef = playerEntry.Key;
            LobbyPlayerState playerState = playerEntry.Value;
            
            var uiEntry = Instantiate(playerEntryPrefab, playerListContainer);
            
            string nickname = playerState.HasNickname?
                playerState.Nickname.ToString() : $"Player {playerRef.PlayerId}";
            bool isLeader = ServerLobbyManager.Instance.IsLobbyLeader(playerRef);
            bool isLocal = ServerLobbyManager.Instance.IsLocalPlayer(playerRef);
            
            uiEntry.Setup(playerRef, nickname, playerState.IsReady, isLeader, isLocal);
            
            _playerEntries[playerRef] = uiEntry;
        }
    }
    
    private void ClearPlayerList()
    {
        foreach (var entry in _playerEntries.Values)
        {
            Destroy(entry.gameObject);
        }
        
        _playerEntries.Clear();
    }
    
    private void ShowConnectPanel()
    {
        connectPanel.SetActive(true);
        preMatchPanel.SetActive(false);

        bool isServer = networkStartupManager != null && networkStartupManager.IsLocalPlayerServer();
        
        joinServerButton.interactable = !isServer;
    }
    
    private void ShowPreMatchPanel()
    {
        connectPanel.SetActive(false);
        preMatchPanel.SetActive(true);
    }

    private void SetupGameModeDropdown()
    {
        if (gameModeDropdown == null)
        {
            Debug.LogError($"[ServerUiManager] GameModeDropdown is null");
            return;
        }
        
        gameModeDropdown.ClearOptions();
        gameModeDropdown.AddOptions(new List<string>
        {
            "Free For All",
            "Conquest",
            "Capture The Flag"
        });
        
        gameModeDropdown.SetValueWithoutNotify(0);
    }

    private void SetConnectedStatus(string newStatus)
    {
        if (connectStatusText == null) return;
        connectStatusText.text = newStatus;
    }
    
    private void SetPreMatchStatus(string newStatus)
    {
        if (preMatchStatusText == null) return;
        preMatchStatusText.text = newStatus;
    }

    private bool ValidateNickname(string nickname)
    {
        if (String.IsNullOrEmpty(nickname))
        {
            SetPreMatchStatus("Nickname cannot be empty");
            return false;
        }
        
        if (nickname.Length > 16)
        {
            SetPreMatchStatus("Nickname cannot be longer than 16 characters");
            return false;
        }
        
        return true;   
    }
    
    private GameModeType GetSelectedGameMode()
    {
        int value = gameModeDropdown != null ? gameModeDropdown.value : 0;

        if (!Enum.IsDefined(typeof(GameModeType), value))
            value = 0;
        
        return (GameModeType) value;
    }

    private void SubscribeToServerManagers()
    {
        if (_subscribedToServerManagers)
            return;
        
        if (ServerLobbyManager.Instance == null)
        {
            Debug.LogError("[ServerUiManager] ServerLobbyManager not ready yet");
            return;
        }

        ServerLobbyManager.Instance.NicknameAccepted += HandleNicknameAccepted;
        ServerLobbyManager.Instance.NicknameRejected += HandleNicknameRejected;
        ServerLobbyManager.Instance.ReadyStateChanged += HandleReadyStateChanged;
        ServerLobbyManager.Instance.LobbyLeaderChanged += HandleLobbyLeaderChanged;
        ServerLobbyManager.Instance.LobbyStateChanged += HandleLobbyStateChanged;
        ServerLobbyManager.Instance.PlayerJoinedLobby += HandlePlayerJoinedLobby;
        ServerLobbyManager.Instance.PlayerLeftLobby += HandlePlayerLeftLobby;
        ServerLobbyManager.Instance.NicknameChanged += HandleNicknameChanged;
        ServerLobbyManager.Instance.LeaveLobbyAccepted += HandleLeaveLobbyAccepted;
        ServerLobbyManager.Instance.LeaveLobbyRejected += HandleLeaveLobbyRejected;
        ServerLobbyManager.Instance.JoinLobbyAccepted += HandleJoinLobbyAccepted;
        ServerLobbyManager.Instance.JoinLobbyRejected += HandleJoinLobbyRejected;
        
        Debug.Log("[ServerUiManager] Client subscribed to ServerLobbyManager events");
        SetConnectedStatus("Ready to join lobby");
        
        _subscribedToServerManagers = true;
        //RefreshPlayerList();
    }

    private void UnsubscribeToServerManagers()
    {
        if (ServerLobbyManager.Instance == null)
            return;

        ServerLobbyManager.Instance.NicknameAccepted -= HandleNicknameAccepted;
        ServerLobbyManager.Instance.NicknameRejected -= HandleNicknameRejected;
        ServerLobbyManager.Instance.ReadyStateChanged -= HandleReadyStateChanged;
        ServerLobbyManager.Instance.LobbyLeaderChanged -= HandleLobbyLeaderChanged;
        ServerLobbyManager.Instance.LobbyStateChanged -= HandleLobbyStateChanged;
        ServerLobbyManager.Instance.PlayerJoinedLobby -= HandlePlayerJoinedLobby;
        ServerLobbyManager.Instance.PlayerLeftLobby -= HandlePlayerLeftLobby;
        ServerLobbyManager.Instance.NicknameChanged -= HandleNicknameChanged;
        ServerLobbyManager.Instance.LeaveLobbyAccepted -= HandleLeaveLobbyAccepted;
        ServerLobbyManager.Instance.LeaveLobbyRejected -= HandleLeaveLobbyRejected;
        ServerLobbyManager.Instance.JoinLobbyAccepted -= HandleJoinLobbyAccepted;
        ServerLobbyManager.Instance.JoinLobbyRejected -= HandleJoinLobbyRejected;
        
        _subscribedToServerManagers = false;   
    }

    private void RefreshConnectPanelStatus()
    {
        bool isServer = networkStartupManager != null && networkStartupManager.IsLocalPlayerServer();
        
        joinServerButton.interactable = !isServer;

        SetConnectedStatus(isServer ? "Server starting..." : "Ready to connect");
    }

    private void RefreshPreMatchControls()
    {
        if (preMatchPanel == null || !preMatchPanel.activeSelf)
            return;

        if (ServerLobbyManager.Instance == null || NetworkMatchManager.Instance == null)
            return;

        bool isLeader = ServerLobbyManager.Instance.IsLocalPlayerLobbyLeader();
        bool matchWaiting = NetworkMatchManager.Instance.MatchState == ServerMatchState.WaitingForPlayers;
        bool canChangeMode = matchWaiting && isLeader && !ServerLobbyManager.Instance.CanStartMatch();
        
        if (gameModeDropdown != null)
            gameModeDropdown.interactable = canChangeMode;
        
        if (confirmGameModeButton != null)
            confirmGameModeButton.interactable = canChangeMode;
        
        if (startMatchButton != null)
            startMatchButton.interactable = matchWaiting && isLeader;
        
        //if (readyButton != null && isReadyToggle != null)
            //readyButton.interactable = matchWaiting && !isReadyToggle.isOn;
    }
    
    #endregion
}
